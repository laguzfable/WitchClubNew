using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UISelectLanguage : MonoBehaviour
{
    public string language;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(()=>{

            PlayerPrefs.SetString("Language", language);

            SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);

        });
    }
}
