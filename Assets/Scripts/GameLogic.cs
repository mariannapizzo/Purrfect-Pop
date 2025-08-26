using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using TMPro;

public class GameLogic : MonoBehaviour
{
    [SerializeField] public GameObject[] cats;
    [SerializeField] public GameObject[] flowers;
    [SerializeField] public float trialDuration;    
    [SerializeField] [Range(1, 7)] public int numberOfTrials;

    // Challenge mode settings
    [SerializeField] public int totalNumberOfTrials = 20;
    [SerializeField] public bool challengeMode;
    [SerializeField] public bool useDistractors;
    [SerializeField] public float trialDurationDecrement = 0.2f;
    [SerializeField] public float distractorSpawnChance = 0.5f;

    private Transform[] spawnPoints;
    private int sessionWrongCats;
    private int totalCorrectCats;
    private GameObject targetsContainer;
    private float betweenTrialsDelay = 1f;
    private GameObject shelving;
    private TextMeshPro points;
    private string basetext = "Score: ";
    private string basetext2 = " Over ";

    public void StartSession(bool isDemo = false) {
        totalCorrectCats = 0;
        sessionWrongCats = 0;
        targetsContainer = new GameObject("Objects");
        points = shelving.GetComponentInChildren<TextMeshPro>();
        points.text = basetext + sessionWrongCats;

        LogGameSettings();
       
        if (isDemo)
            StartCoroutine(RunDemo());
        else {
            StartCoroutine(challengeMode 
                ? RunChallengeScenario() 
                : RunScenario());
        }
    }

    private void LogGameSettings() {
        if (challengeMode) {
            Actions.OnEvent("GAME MODE: Challenge Mode");
            Actions.OnEvent($"Total number of trials: {totalNumberOfTrials}");
            Actions.OnEvent($"Initial trial duration: {(float.IsInfinity(trialDuration) ? "Infinite" : trialDuration.ToString("F1"))} seconds");

            Actions.OnEvent(trialDurationDecrement > 0
                ? $"Trial duration decrement enabled: -{trialDurationDecrement} seconds per trial (minimum 1 second)"
                : "Trial duration decrement disabled: constant duration throughout all trials");

            Actions.OnEvent($"Distractors enabled: {useDistractors}");
            
            if (useDistractors)
                Actions.OnEvent($"Distractor spawn probability: {distractorSpawnChance * 100:F1}%");
        }
        else {
            Actions.OnEvent("GAME MODE: Simple Mode");
            Actions.OnEvent($"Number of trials per subsession: {numberOfTrials}");
            Actions.OnEvent($"Trial duration: {(float.IsInfinity(trialDuration) ? "Infinite" : trialDuration.ToString("F1"))} seconds");
        }
    }

    private void ProcessUserInput(string objectName, Vector3 objectPosition) {
        if (objectName.Contains("cat"))
            sessionWrongCats++;
        
        points.text = basetext + sessionWrongCats;
        Actions.OnEvent(objectName + " touched at " + objectPosition + " at time " + DateTime.Now.ToString("hh:mm:ss"));
    }

    #region demo
        private IEnumerator RunDemo() {
            Actions.OnEvent?.Invoke("STARTING DEMO SESSION");
            Actions.OnObjectTouched += ProcessUserInput;
            
            yield return StartCoroutine(DemoSubSession());
            
            Actions.OnDemoEnd?.Invoke();
            Actions.OnObjectTouched -= ProcessUserInput;
            Actions.OnEvent?.Invoke("ENDING DEMO SESSION");

            sessionWrongCats = 0;
            totalCorrectCats = 0;

            yield return new WaitForSeconds(2f);
            
            Destroy(targetsContainer);
        }

        private IEnumerator DemoSubSession() {
            for (int i = 0; i < 5; ++i) {
                int nTargets = Random.Range(1, 5);
                List<GameObject> targets = new List<GameObject>();
                List<GameObject> randomTargets = GetShuffledTargetList();
                int pos1, pos2, pos3, pos4;

                pos1 = Random.Range(0, spawnPoints.Length);
                targets.Add(SpawnTarget(randomTargets[0], spawnPoints[pos1].position));
                randomTargets.RemoveAt(0);
                if (nTargets > 1) {
                    do {
                        pos2 = Random.Range(0, spawnPoints.Length);
                    } while (pos2 == pos1);
                    targets.Add(SpawnTarget(randomTargets[0], spawnPoints[pos2].position));
                    randomTargets.RemoveAt(0);

                    if (nTargets > 2) {
                        do {
                            pos3 = Random.Range(0, spawnPoints.Length);
                        } while (pos3 == pos1 || pos3 == pos2);
                        targets.Add(SpawnTarget(randomTargets[0], spawnPoints[pos3].position));
                        randomTargets.RemoveAt(0);

                        if (nTargets > 3) {
                            do {
                                pos4 = Random.Range(0, spawnPoints.Length);
                            } while (pos4 == pos1 || pos4 == pos2 || pos4 == pos3);
                            targets.Add(SpawnTarget(randomTargets[0], spawnPoints[pos4].position));
                            randomTargets.RemoveAt(0);
                        }
                    }
                }

                float timeLeft = trialDuration;
                while (timeLeft > 0 && targets.Any(target => target != null)) {
                    yield return new WaitForSeconds(1f);
                    timeLeft -= 1f;
                }

                foreach (GameObject target in targets)
                    Destroy(target);
            }
        }
    #endregion
    
    #region simple_mode
        private IEnumerator RunScenario() {
            Actions.OnEvent?.Invoke("STARTING SESSION");
            Actions.OnObjectTouched += ProcessUserInput;
            Actions.OnEvent?.Invoke("STARTING SUBSESSION");
            
            yield return StartCoroutine(FirstSubSession());
            
            Actions.OnEvent?.Invoke("END SUBSESSION");
            Actions.OnEvent?.Invoke("STARTING SUBSESSION");
            
            yield return StartCoroutine(SecondSubSession());
            
            Actions.OnEvent?.Invoke("END SUBSESSION");
            Actions.OnEvent?.Invoke("STARTING SUBSESSION");
            
            yield return StartCoroutine(ThirdSubSession());
            
            Actions.OnEvent?.Invoke("END SUBSESSION");
            Actions.OnEvent?.Invoke("STARTING SUBSESSION");
            
            yield return StartCoroutine(FourthSubSession());
            
            Actions.OnEvent?.Invoke("END SUBSESSION");
            Actions.OnObjectTouched -= ProcessUserInput;
            Actions.OnEvent?.Invoke("ENDING SESSION");
            
            yield return new WaitForSeconds(2f);
        }
        
        private IEnumerator FirstSubSession() {
            int subsessionCats = 0;

            GameObject[] targets = GetShuffledTargetList().ToArray();
            for (int i = 0; i < numberOfTrials; i++) {
                Actions.OnEvent("Trial " + (i + 1) + " start time: " + DateTime.Now.ToString("hh:mm:ss"));

                GameObject spawnedObject = SpawnTarget(targets[i], spawnPoints[Random.Range(0, spawnPoints.Length)].position);

                if (spawnedObject.name.Contains("cat"))
                    subsessionCats++;

                float timeLeft = trialDuration;
                while (timeLeft > 0 && spawnedObject) {
                    yield return new WaitForSeconds(1f);
                    timeLeft -= 1f;
                }

                Destroy(spawnedObject);

                yield return new WaitForSeconds(betweenTrialsDelay);
            }
        }

        private IEnumerator SecondSubSession() {
            int subsessionCats = 0;

            List<GameObject> shuffledList = GetShuffledTargetList();
            List<(GameObject, GameObject)> pairs = new List<(GameObject, GameObject)> {
                (flowers[0], cats[Random.Range(0, 3)]),
                (flowers[0], cats[Random.Range(0, 3)]),
                (flowers[0], cats[Random.Range(0, 3)]),
                (flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
                (flowers[0], flowers[Random.Range(1, 3)]),
                (cats[Random.Range(0, 3)], cats[Random.Range(0, 3)]),
                (shuffledList[0], shuffledList[1])
            };

            pairs.Shuffle();
            
            for (int i = 0; i < numberOfTrials; i++) {
                Actions.OnEvent("Trial " + (i + 1) + " start time: " + DateTime.Now.ToString("hh:mm:ss"));

                int pos1, pos2;
                pos1 = Random.Range(0, spawnPoints.Length);
                do {
                    pos2 = Random.Range(0, spawnPoints.Length);
                } while (pos2 == pos1);

                GameObject[] targetPairs = {
                    SpawnTarget(pairs[i].Item1, spawnPoints[pos1].position),
                    SpawnTarget(pairs[i].Item2, spawnPoints[pos2].position)
                };

                foreach (GameObject obj in targetPairs)
                    if (obj.name.Contains("cat"))
                        subsessionCats++;

                float timeLeft = trialDuration;
                while (timeLeft > 0 && (targetPairs[0] || targetPairs[1])) {
                    yield return new WaitForSeconds(1f);
                    timeLeft -= 1f;
                }

                foreach (GameObject obj in targetPairs)
                    Destroy(obj);

                yield return new WaitForSeconds(betweenTrialsDelay);
            }
        }

        private IEnumerator ThirdSubSession() {
            int subsessionCats = 0;

            List<GameObject> shuffledList = GetShuffledTargetList();
            List<(GameObject, GameObject, GameObject)> triplets = new List<(GameObject, GameObject, GameObject)> {
                (flowers[0], cats[Random.Range(0, 3)], cats[Random.Range(0, 3)]),
                (flowers[0], cats[Random.Range(0, 3)], flowers[Random.Range(1, 3)]),
                (flowers[0], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
                (cats[Random.Range(0, 3)], cats[Random.Range(0, 3)], cats[Random.Range(0, 3)]),
                (flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
                (cats[Random.Range(0, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
                (shuffledList[0], shuffledList[1], shuffledList[2])
            };

            triplets.Shuffle();

            for (int i = 0; i < numberOfTrials; i++) {
                Actions.OnEvent("Trial " + (i + 1) + " start time: " + DateTime.Now.ToString("hh:mm:ss"));

                int pos1, pos2, pos3;
                pos1 = Random.Range(0, spawnPoints.Length);
                do {
                    pos2 = Random.Range(0, spawnPoints.Length);
                } while (pos2 == pos1);
                do {
                    pos3 = Random.Range(0, spawnPoints.Length);
                } while (pos3 == pos1 || pos3 == pos2);


                GameObject[] targetTriplets = {
                    SpawnTarget(triplets[i].Item1, spawnPoints[pos1].position),
                    SpawnTarget(triplets[i].Item2, spawnPoints[pos2].position),
                    SpawnTarget(triplets[i].Item3, spawnPoints[pos3].position)
                };

                foreach (GameObject obj in targetTriplets) 
                    if (obj.name.Contains("cat"))
                        subsessionCats++;

                float timeLeft = trialDuration;
                while (timeLeft > 0 && (targetTriplets[0] || targetTriplets[1] || targetTriplets[2])) {
                    yield return new WaitForSeconds(1f);
                    timeLeft -= 1f;
                }

                foreach (GameObject obj in targetTriplets)
                    Destroy(obj);

                yield return new WaitForSeconds(betweenTrialsDelay); // Pausa tra un trial e l'altro
            }
        }

        private IEnumerator FourthSubSession() {
            int subsessionCats = 0;

            List<GameObject> shuffledList = GetShuffledTargetList();
            List<(GameObject, GameObject, GameObject, GameObject)> quadruplets = new List<(GameObject, GameObject, GameObject, GameObject)> {
            (flowers[0], cats[Random.Range(0, 3)], cats[Random.Range(0, 3)], cats[Random.Range(0, 3)]),
            (flowers[0], cats[Random.Range(0, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
            (flowers[0], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
            (cats[Random.Range(0, 3)], cats[Random.Range(0, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
            (cats[Random.Range(0, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
            (flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)], flowers[Random.Range(1, 3)]),
            (shuffledList[0], shuffledList[1], shuffledList[2], shuffledList[3])
        };

            quadruplets.Shuffle();

            for (int i = 0; i < numberOfTrials; i++) {
                Actions.OnEvent("Trial " + (i + 1) + " start time: " + DateTime.Now.ToString("hh:mm:ss"));

                int[] positions = new int[4];
                for (int j = 0; j < 4; j++) {
                    do {
                        positions[j] = Random.Range(0, spawnPoints.Length);
                    } while (positions.Take(j).Contains(positions[j])); // Assicura unicità
                }

                GameObject[] targetQuadruplets = {
                SpawnTarget(quadruplets[i].Item1, spawnPoints[positions[0]].position),
                SpawnTarget(quadruplets[i].Item2, spawnPoints[positions[1]].position),
                SpawnTarget(quadruplets[i].Item3, spawnPoints[positions[2]].position),
                SpawnTarget(quadruplets[i].Item4, spawnPoints[positions[3]].position)
                };

                foreach (GameObject obj in targetQuadruplets) 
                    if (obj.name.Contains("cat"))
                        subsessionCats++;

                float timeLeft = trialDuration;
                while (timeLeft > 0 && targetQuadruplets.Any(obj => obj != null)) {
                    yield return new WaitForSeconds(1f);
                    timeLeft -= 1f;
                }

                foreach (GameObject obj in targetQuadruplets)
                    if (obj) Destroy(obj);
            }

            points.text = basetext + sessionWrongCats + basetext2 + totalCorrectCats;

            Actions.OnEvent($"SIMPLE SESSION FINAL OUTCOME: Cats touched: ({sessionWrongCats}/{totalCorrectCats}) - Total cats spawned: {totalCorrectCats}");

            sessionWrongCats = 0;

            yield return new WaitForSeconds(2f);
            Actions.OnQuit?.Invoke();
        }
    #endregion

    #region challenge_mode
        private IEnumerator RunChallengeScenario() {
            totalCorrectCats=0;
            Actions.OnEvent?.Invoke("STARTING CHALLENGE SESSION");
            Actions.OnObjectTouched += ProcessUserInput;
            float currentTrialDuration = trialDuration;
            
            for (int i = 0; i < totalNumberOfTrials; i++) {
                Actions.OnEvent($"Challenge Trial {i + 1} start time: {DateTime.Now:hh:mm:ss} - Duration: {(float.IsInfinity(currentTrialDuration) ? "Infinite" : currentTrialDuration.ToString("F1"))} seconds");
                
                int nTargets = Random.Range(1, 5);
                List<GameObject> targets = new List<GameObject>();
                List<GameObject> randomTargets = GetShuffledTargetList();
                List<int> usedPositions = new List<int>();
                
                for (int j = 0; j < nTargets; j++) {
                    int pos;
                    do {
                        pos = Random.Range(0, spawnPoints.Length);
                    } while (usedPositions.Contains(pos));
                    usedPositions.Add(pos);
                    targets.Add(SpawnTarget(randomTargets[j], spawnPoints[pos].position));
                }

                GameObject distractor = null;
                if (useDistractors && Random.value < distractorSpawnChance) {
                    int pos;
                    do {
                        pos = Random.Range(0, spawnPoints.Length);
                    } while (usedPositions.Contains(pos));
                    usedPositions.Add(pos);
                    distractor = SpawnTarget(randomTargets[nTargets], spawnPoints[pos].position);
                    MakeDistractor(distractor);
                    Actions.OnEvent("Distractor spawned in trial " + (i + 1));
                }

                float timeLeft = currentTrialDuration;
                while (timeLeft > 0 && targets.Any(target => target != null)) {
                    yield return new WaitForSeconds(1f);
                    timeLeft -= 1f;
                }

                foreach (GameObject target in targets)
                    if (target != null)
                        Destroy(target);
                if (distractor != null)
                    Destroy(distractor);
                
                
                if (trialDurationDecrement > 0 && !float.IsInfinity(currentTrialDuration))
                    currentTrialDuration = Mathf.Max(1f, currentTrialDuration - trialDurationDecrement);
               
                yield return new WaitForSeconds(betweenTrialsDelay);
            }
            points.text = basetext + sessionWrongCats + basetext2 + totalCorrectCats;

            Actions.OnEvent($"CHALLENGE SESSION FINAL OUTCOME: Cats touched: ({sessionWrongCats}/{totalCorrectCats}) - Total cats spawned: {totalCorrectCats}");
            Actions.OnEvent?.Invoke("ENDING CHALLENGE SESSION");
            Actions.OnObjectTouched -= ProcessUserInput;
            
            yield return new WaitForSeconds(15f);
            
            Destroy(targetsContainer);
            Actions.OnQuit?.Invoke();
        }
        
        private void MakeDistractor(GameObject obj) {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            Renderer[] renda = obj.GetComponentsInChildren<Renderer>();
            if (renda[0] != null) {
                foreach (var rend in renda) 
                    rend.material.color = Color.Lerp(rend.material.color, Color.red, 0.4f);               
            }
        }
    #endregion


    private GameObject SpawnTarget(GameObject Prefab, Vector3 Position) {
        if (Prefab.name.Contains("cat"))
           totalCorrectCats++;

        GameObject newTarget = Instantiate(Prefab, targetsContainer.transform, true);
        newTarget.transform.position = Position;
        newTarget.transform.rotation = Quaternion.LookRotation(shelving.transform.forward);
        return newTarget;
    }

    private List<GameObject> GetShuffledTargetList() {
        List<GameObject> shuffledList = new List<GameObject>();
        List<GameObject> origin = cats.ToList();
        origin.AddRange(flowers.ToList());

        for (int i = 0; i < cats.Length + flowers.Length; ++i) {
            int pick = Random.Range(0, origin.Count);
            shuffledList.Add(origin[pick]);
            origin.RemoveAt(pick);
        }
        shuffledList.Add(shuffledList[Random.Range(0, shuffledList.Count)]);
        return shuffledList;
    }
    

    public void SetSpawnPoints(List<Transform> Points) {
        spawnPoints = Points.ToArray();
    }

    public void SetShelving(GameObject ShelvingInstance) {
        shelving = ShelvingInstance;
    }
}