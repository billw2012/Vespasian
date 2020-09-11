﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public GameLogic gameLogic;
    public GameObject nextButton;

    // Start is called before the first frame update
    void Start()
    {
        this.nextButton.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(!this.nextButton.activeSelf && FindObjectsOfType<Objective>().Where(o => o.required).All(o => o.complete))
        {
            this.nextButton.SetActive(true);
        }
    }
}