using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Victory : MonoBehaviour
{
    public TextMeshProUGUI victoryText;
    // Start is called before the first frame update
    void Start()
    {
        if (GameController.whiteWon)
            victoryText.text = "White Team Wins!";
        else
            victoryText.text = "Black Team Wins!";

    }
}
