using UnityEngine;

public class Playmusic : MonoBehaviour
{
    private AudioSource musicSource;

    private void Start()
    {
        musicSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            musicSource.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            musicSource.Stop();
        }
    }
}
