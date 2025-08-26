using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] public GameObject shelving;
    [SerializeField] public Material spacerMaterial;
    
    [Header("Parameters")]
    [SerializeField] public float perspectiveHeightOffset = 0.1f;
    
    [Header("Controls")]
    [SerializeField] public KeyCode calibrationKey = KeyCode.C;
    [SerializeField] public KeyCode demoKey = KeyCode.D;
    [SerializeField] public KeyCode experimentKey = KeyCode.E;
    [SerializeField] public KeyCode quitKey = KeyCode.Q;

    private TextMeshPro points;
    private string baseText = "Score: ";
    private GameObject vrAvatar;
    private Transform[] hands = new Transform[2];
    private GameLogic gameLogic;
    private DataLogger logger;
    private GameObject shelvingInstance;
    private GameObject spacer;
    private Vector3 shelvingExtents;
    private MeshRenderer fadePanelVR;
    private int fadeDuration = 3;
    private float targetShelvingOffset;
    private bool requestedCalibration;
    private bool requestedStart;
    private bool requestedQuit;
    private bool isTimeToCalibrate;
    private bool isCalibrated;
    
    private void Start() {
        points = shelving.GetComponentInChildren<TextMeshPro>();
        
        vrAvatar = GameObject.Find("Leonard");
        fadePanelVR = GameObject.Find("FadePanelVR").GetComponent<MeshRenderer>();
        gameLogic = GetComponent<GameLogic>();
        
        Actions.OnDemoEnd += OnDemoEnd;
        StartCoroutine(FadedGameStart());
    }

    private void Update() {
        if (isTimeToCalibrate) {
            SetHandTransform();
            SpawnShelving();

            isTimeToCalibrate = false;
            requestedCalibration = false;
            isCalibrated = true;
        }
        else if (Input.GetKeyDown(calibrationKey) && !requestedCalibration) {
            requestedCalibration = true;
            isCalibrated = false;
            StartCoroutine(DelayedCalibration());
        }

        if (Input.GetKeyDown(demoKey) && !requestedStart) {
            if (!isCalibrated) {
                Debug.LogWarning("Avatar not calibrated yet.");
                return;
            }

            requestedStart = true;
            StartCoroutine(DelayedSessionStart(true));
        }

        if (Input.GetKeyDown(experimentKey) && !requestedStart) {
            if (!isCalibrated) {
                Debug.LogWarning("Avatar not calibrated yet.");
                return;
            }

            requestedStart = true;
            StartCoroutine(DelayedSessionStart());
        }
       

        if (Input.GetKeyDown(quitKey) && !requestedQuit) {
            requestedQuit = true;
            Actions.OnQuit?.Invoke();
        }
    }
    
    private IEnumerator FadedGameStart() {
        fadePanelVR.material.SetColor(Shader.PropertyToID("_Color"), new Color(0f, 0f, 0f, 1f));
        
        yield return new WaitForSeconds(1f);
        yield return Fade(fadePanelVR, 1.0f, 0.0f, fadeDuration);
        points.text = baseText + "0";
        Actions.OnEvent?.Invoke("=== EXPERIMENT START ===");
    }
    
    private IEnumerator DelayedSessionStart(bool isDemo = false) {
        yield return new WaitForSeconds(1f);
        
        gameLogic.enabled = true;
        gameLogic.StartSession(isDemo);
    }

    #region shelving_calibration
        private void SetHandTransform() {
            Animator animator = vrAvatar.GetComponent<Animator>();
            hands[0] = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
            hands[1] = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
        }
        
        private IEnumerator DelayedCalibration() {
            yield return new WaitForSeconds(1f);
            isTimeToCalibrate = true;
        }
        
        private void SpawnShelving() {
            if (!shelvingInstance) {
                shelvingInstance = Instantiate(shelving, Vector3.zero, Quaternion.identity);
                shelvingExtents = shelvingInstance.GetComponentInChildren<BoxCollider>().bounds.extents;
                
                GameObject target = shelvingInstance.transform.Find("Mid").gameObject;
                targetShelvingOffset = target.transform.position.y - shelvingInstance.transform.position.y + perspectiveHeightOffset;
                target.SetActive(false);
                
                List<Transform> targetPoints = new List<Transform>();
                foreach(Transform child in shelvingInstance.transform.Find("Targets"))
                    targetPoints.Add(child);
                
                gameLogic.SetShelving(shelvingInstance);
                gameLogic.SetSpawnPoints(targetPoints);
                
                shelvingInstance.transform.SetParent(GameObject.Find("Environment").transform);
            }

            Vector3 midhands = (hands[0].position - hands[1].position) * 0.5f + hands[1].position;

            Vector3 shelvingPos = new Vector3(midhands.x, midhands.y - targetShelvingOffset, midhands.z);
            shelvingInstance.transform.position = shelvingPos;

            if (!spacer) {
                spacer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spacer.GetComponent<Renderer>().material = spacerMaterial;
                spacer.transform.SetParent(GameObject.Find("Environment").transform);
            }
            
            float spacerYSize = shelvingPos.y - shelvingExtents.y;
            spacer.transform.position = new Vector3(shelvingPos.x, spacerYSize / 2, shelvingPos.z);
            spacer.transform.localScale = new Vector3(shelvingExtents.x * 2, spacerYSize, shelvingExtents.z * 2);

            Vector3 fwProj = Vector3.ProjectOnPlane(GameObject.Find("Main Camera").transform.forward, Vector3.up)
                .normalized;
            shelvingInstance.transform.rotation = Quaternion.LookRotation(fwProj);
            spacer.transform.rotation = Quaternion.LookRotation(fwProj);
        }
    #endregion
    
    #region action_callbacks
        private void OnDemoEnd() {
            requestedStart = false;
        }
    #endregion
    
    private IEnumerator Fade(MeshRenderer fadePanel, float alphaIn, float alphaOut, float fadeDuration) {
        float timer = 0;
        Color newColor = new Color(0f, 0f, 0f, alphaIn);
        while (timer <= fadeDuration) {
            newColor.a = Mathf.Lerp(alphaIn, alphaOut, timer / fadeDuration);
            fadePanel.material.SetColor(Shader.PropertyToID("_Color"), newColor);
            timer += Time.deltaTime;
            yield return null;
        }

        newColor = new Color(0f, 0f, 0f, alphaOut);
        fadePanel.material.SetColor(Shader.PropertyToID("_Color"), newColor);
    }
}