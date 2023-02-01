using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public TMP_InputField generations;
    public TMP_InputField populationSize;
    public TMP_InputField movements;
    public TMP_InputField elitism;
    public TMP_InputField mutationProb;
    public TMP_InputField speed;
    public TMP_Dropdown typeOfDistanceDropdown;
    public TMP_Dropdown mapDropdown;


    private void Start()
    {
        // initialize input fields with default values
        generations.text = Controller.Settings.generations.ToString();
        populationSize.text = Controller.Settings.populationSize.ToString();
        mutationProb.text = Controller.Settings.mutationProb.ToString();
        movements.text = Controller.Settings.movements.ToString();
        elitism.text = Controller.Settings.elitism.ToString();
        speed.text = Controller.Settings.speed.ToString();
        typeOfDistanceDropdown.value = (int)Controller.Settings.typeOfDistance;
        mapDropdown.value = (int)Controller.Settings.map;
    }

    public void LoadGameScene()
    {
        SetSettings();

        SceneManager.LoadSceneAsync("Maps");


    }

    private void SetSettings()
    {
        Controller.Settings.generations = int.Parse(generations.text);
        Controller.Settings.populationSize = int.Parse(populationSize.text);
        Controller.Settings.movements = int.Parse(movements.text);
        Controller.Settings.elitism = int.Parse(elitism.text);
        Controller.Settings.mutationProb = float.Parse(mutationProb.text);
        Controller.Settings.speed = float.Parse(speed.text);
        Controller.Settings.typeOfDistance = (TypeOfDistance)typeOfDistanceDropdown.value;
        Controller.Settings.map = (Map)mapDropdown.value;
    }
}
