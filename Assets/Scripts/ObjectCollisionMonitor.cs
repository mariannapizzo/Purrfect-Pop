using System.Collections;
using UnityEngine;

public class ObjectCollisionMonitor : MonoBehaviour
{
    private ParticleSystem Smoke;
    private AudioSource Source;
    private bool HasBeenAnimated;

    private void Start() {
        Smoke = transform.Find("Smoke").GetComponent<ParticleSystem>();
        Smoke.gameObject.SetActive(false);
        Source = transform.Find("Audio Source").GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Player") && !HasBeenAnimated) 
            StartCoroutine(DelayedDestroy());
    }

    private IEnumerator DelayedDestroy() {
        HasBeenAnimated = true;
        
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            renderer.enabled = false;
        
        Smoke.gameObject.SetActive(true);
        Smoke.Play();
        Source.Play();

        while (Source.isPlaying && Smoke.isPlaying)
            yield return null;
        
        Actions.OnObjectTouched?.Invoke(name.Replace("(Clone)", ""), transform.position);
        Destroy(gameObject);
    }
}
