using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectLanguageScene : MonoBehaviour
{

    [SerializeField] GameObject selectLanguage;


    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        selectLanguage.SetActive(true);
#else
        if(PlayerPrefs.HasKey("Language"))
        {
            SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
        }
        else
        {
            selectLanguage.SetActive(true);
        }
#endif
    }
}
