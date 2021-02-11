// using System.Collections;
// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class MissionMapSystemFactory : MonoBehaviour, IMissionFactory, ISavable
// {
//     public GameObject boardUIPrefab;
//     public GameObject activeUIPrefab;
//
//     [Saved]
//     public int missionCounter = 0;
//
//     private void Awake()
//     {
//         ComponentCache.FindObjectOfType<SaveSystem>().RegisterForSaving(this);
//     }
//
//     public IMissionBase Generate(RandomX rng)
//     {
//         return new MissionMapSystem(typeof(MissionMapSystemFactory).Name, $"Mission {++this.missionCounter}");
//     }
//
//     public GameObject CreateBoardUI(Missions missions, IMissionBase mission, Transform parent)
//     {
//         var missionTyped = mission as MissionMapSystem;
//         var ui = Object.Instantiate(this.boardUIPrefab, parent);
//         ui.transform.Find("Description").GetComponent<TMP_Text>().text = missionTyped.Description;
//         ui.transform.Find("Take").GetComponent<Button>().onClick.AddListener(() => missions.Take(mission));
//         return ui;
//     }
//
//     public GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent)
//     {
//         var missionTyped = mission as MissionMapSystem;
//         var ui = Object.Instantiate(this.activeUIPrefab, parent);
//         ui.transform.Find("Description").GetComponent<TMP_Text>().text = missionTyped.Description;
//         ui.transform.Find("Hand In").GetComponent<Button>().onClick.AddListener(() => missions.HandIn(mission));
//         return ui;
//     }
// }
//
// [RegisterSavableType]
// public class MissionMapSystem : IMissionBase
// {
//     [Saved]
//     public bool IsComplete { get; set; }
//
//     [Saved]
//     public string Factory { get; set; }
//
//     [Saved]
//     public string Description { get; set; }
//
//     public MissionMapSystem() { }
//
//     public MissionMapSystem(string factory, string description)
//     {
//         this.Factory = factory;
//         this.Description = description;
//     }
//
//     public void Update(Missions missions)
//     {
//         // this.IsComplete = false;
//     }
// }
