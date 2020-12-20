using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Class which manages the star system UI
public class StarSystemUI : MonoBehaviour
{
    [Tooltip("Where system scheme will be drawn")]
    public RectTransform schemeRoot;

    [Tooltip("Prefab which will be used to generate the star system scheme")]
    public GameObject schemeBodyPrefab;

    public TextMeshProUGUI selectedBodyDescription_tmp;
    public TextMeshProUGUI selectedBodyName_tmp;

    private MapComponent mapComponent;
    private DataCatalog playerData;

    private SolarSystem system;

    // List with star system scheme elements
    private List<GameObject> schemeElements = null;

    // Start is called before the first frame update
    private void Awake()
    {
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.playerData = FindObjectOfType<PlayerController>().GetComponent<DataCatalog>();
    }

    public void OnEnable()
    {
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.GenerateSchemeFromSystem(this.mapComponent.currentSystem);
    }

    public void test()
    {
        this.GenerateSchemeFromSystem(this.mapComponent.currentSystem);
    }


    // Generates elements of this UI from a solar system
    private void GenerateSchemeFromSystem(SolarSystem system)
    {
        Debug.Log("GenerateSchemeFromSystem");

        // To be sure...
        if (system == null)
        {
            Debug.LogError("GenerateFromSystem: system is null!");
            return;
        }

        // Delete previous scheme elements
        if (this.schemeElements != null)
        {
            foreach (var element in this.schemeElements)
            {
                Object.Destroy(element);
            }
            this.schemeElements = null; // Delete the list too
        }

        this.system = system;

        // Generate elements from system
        var elements = new List<GameObject>();
        this.schemeElements = elements;

        // Local function which initializes transform of the scheme element
        void InitSchemeBodyTransform(GameObject obj, float x, float y)
        {
            var rectTransform = obj.GetComponent<RectTransform>();       
            rectTransform.SetParent(this.schemeRoot.GetComponent<RectTransform>());
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(x, y);
            // Don't know why it creates it with != 1 scale otherwise!
            rectTransform.localScale = Vector3.one; 
        }

        // Where child objects will start getting rendered at
        float currentXPos = 0;
        float currentYPos = 0;

        // Generate scheme element for star
        var mainBody = system.main;
        var starGameObject = Instantiate(this.schemeBodyPrefab);
        elements.Add(starGameObject);
        InitSchemeBodyTransform(starGameObject, currentXPos, currentYPos);
        
        // Setup its visuals
        this.InitSchemeBodyProperties(starGameObject, mainBody);
        currentXPos += (starGameObject.GetComponent<RectTransform>().rect.width + 100.0f);

        // Iterate star's planets
        foreach (var planet in system.main.children)
        {
            var planetGameObject = Instantiate(this.schemeBodyPrefab);
            elements.Add(planetGameObject);
            InitSchemeBodyTransform(planetGameObject, currentXPos, currentYPos);
            this.InitSchemeBodyProperties(planetGameObject, planet);

            currentYPos -= (planetGameObject.GetComponent<RectTransform>().rect.height + 0.0f);

            // Iterate planet's moons
            // Todo what to do with binary systems?
            foreach (var moon in planet.children)
            {
                var moonGameObject = Instantiate(this.schemeBodyPrefab);
                elements.Add(moonGameObject);
                InitSchemeBodyTransform(moonGameObject, currentXPos, currentYPos);
                this.InitSchemeBodyProperties(moonGameObject, moon);

                // Right now there is some vertical gap in these prefabs itself
                currentYPos -= moonGameObject.GetComponent<RectTransform>().rect.height; 
            }

            // Add some offset between them
            currentXPos += (planetGameObject.GetComponent<RectTransform>().rect.width + 50.0f); 
            currentYPos = 0;
        }
    }

    // Sets generic body properties in the scheme view, such as...
    // Body's displayed name, appearence, ...
    private void InitSchemeBodyProperties(GameObject schemeElement, OrbitingBody bodyData)
    {
        var bodyComponent = schemeElement.GetComponent<StarSystemUIBody>();
        bodyComponent.bodyName = bodyData.bodyRef.ToString(); // Read the body name here when we have it
        bodyComponent.starSystemUI = this; // It must point back to call functions when clicked
        bodyComponent.actualBody = bodyData;
    }

    // Called when we click on scheme body
    // body - the body which was clicked
    public void OnSchemeBodyClick(StarSystemUIBody body)
    {
        //Debug.Log($"OnSchemeBodyClick: {body}");

        // Enable selector only on one of the elements
        foreach (var element in this.schemeElements)
        {
            var uiBodyComponent = element.GetComponent<StarSystemUIBody>();
            bool enable = uiBodyComponent == body;
            //Debug.Log($"Enable: {enable}");
            uiBodyComponent.selectorImage.enabled = enable;
        }

        // Fill the data in the info panel
        var actualBody = body.actualBody;
        this.selectedBodyName_tmp.text = actualBody.randomKey.ToString();
        
        var knownDataMask = this.playerData.GetData(actualBody.bodyRef);
        //var knownDataStr = new List<string>{"Type: Planet"};
        var knownData = actualBody.GetData(knownDataMask);
        this.selectedBodyDescription_tmp.text = string.Join("\n", knownData.Select(d => $"{d.name}: {d.entry}"));
    }
}
