using Naninovel;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    [SerializeField] TargetScript[] scriptArr;

    [SerializeField] string variableName;
    
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(()=>{

            int targetScriptIndex = 0;
            Engine.GetService<ICustomVariableManager>().TryGetVariableValue<int>(variableName, out targetScriptIndex);

            GameObject.FindObjectOfType<RestRoom>().SelectCharacter(scriptArr[targetScriptIndex].scriptName, scriptArr[targetScriptIndex].scriptLabel);
            Engine.GetService<ICustomVariableManager>().TrySetVariableValue<int>(variableName, targetScriptIndex+1);
        });
    }


    [System.Serializable]
    public class TargetScript
    {
        public string scriptName;
        public string scriptLabel;
    }
}
