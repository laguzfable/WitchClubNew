using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GotoBattle : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(()=>
        {
            SceneManager.LoadScene("CombatScene");
        });
    }
}
