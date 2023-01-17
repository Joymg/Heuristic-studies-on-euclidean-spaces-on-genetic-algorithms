using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public TMP_InputField generations;
    public TMP_InputField populationSize;
    public TMP_InputField movements;
    public TMP_InputField mutationProb;
    public TMP_InputField speed;


    private void Start()
    {
        // initialize input fields with default values
        generations.text = Controller.Settings.generations.ToString();
        populationSize.text = Controller.Settings.populationSize.ToString();
        mutationProb.text = Controller.Settings.mutationProb.ToString();
        movements.text = Controller.Settings.movements.ToString();
        speed.text = Controller.Settings.speed.ToString();
    }

    public void LoadGameScene()
    {
        SetSettings();

        SceneManager.LoadScene("GameScene");
    }

    private void SetSettings()
    {
        Controller.Settings.generations = int.Parse(generations.text);
        Controller.Settings.populationSize = int.Parse(generations.text);
        Controller.Settings.movements = int.Parse(generations.text);
        Controller.Settings.mutationProb = int.Parse(generations.text);
        Controller.Settings.speed = int.Parse(generations.text);
    }
}
