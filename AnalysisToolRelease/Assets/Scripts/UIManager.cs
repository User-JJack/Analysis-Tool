using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] MapAnalyser analyser;
    [SerializeField] GameObject toggle;

    [Header("UI")]
    [SerializeField] TMP_InputField mapToAnalyse;
    [SerializeField] TMP_Dropdown dropDown;
    [SerializeField] Transform scrollPanel;
    [SerializeField] TMP_InputField dataInput;
    [SerializeField] Toggle isNumeric;

    private int scrollNo = 0;
    private float hOffset= 20;

    private void Awake()
    {
        hOffset = toggle.GetComponent<RectTransform>().sizeDelta.y;
    }

    public void AddMapOption(string mapName)
    {
        GameObject newToggle = Instantiate(toggle);
        newToggle.GetComponentInChildren<Text>().text = mapName;
        newToggle.transform.SetParent(scrollPanel, false);
        newToggle.transform.localPosition -= new Vector3(0,  scrollNo * hOffset, 0 );
        newToggle.transform.localScale = Vector2.one;
        scrollNo++;          
    }

    public void Calculate()
    {
        List<string> references = new List<string>();
        foreach(Transform child in scrollPanel)
        {
            if(child.GetComponent<Toggle>().isOn)
            {
                references.Add(child.GetComponentInChildren<Text>().text);
            }
        }

        if(dropDown.options[dropDown.value].text.Equals("Output Matches"))
        {
            foreach(string name in references)
            {
                analyser.FindMatches(mapToAnalyse.text, name, false);
            }
        }
        else if (dropDown.options[dropDown.value].text.Equals("Analyze Data"))
        {
            analyser.AnalyseData(mapToAnalyse.text, dataInput.text, isNumeric.isOn, references.ToArray());
        }
    }

    public void ToggleDataInput()
    {
        if (dropDown.options[dropDown.value].text.Equals("Analyze Data"))
        {
            dataInput.gameObject.SetActive(true);
            isNumeric.gameObject.SetActive(true);
        } else
        {
            dataInput.gameObject.SetActive(false);
            isNumeric.gameObject.SetActive(false);
        }
    }

}
