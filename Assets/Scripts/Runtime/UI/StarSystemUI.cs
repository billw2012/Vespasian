using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// Class which manages the star system UI
public class StarSystemUI : MonoBehaviour, IUILayer
{
    [Tooltip("Where system scheme will be drawn")]
    public RectTransform schemeRoot;

    [Tooltip("Prefab which will be used to generate the star system scheme")]
    public GameObject schemeBodyPrefab;

    public GameObject defaultBodyIconPrefab;
    public GameObject unknownBodyIconPrefab;
    
    public TMP_Text systemLabel;
    public TMP_Text selectedBodyDescriptionLabel;
    public TMP_Text selectedBodyNameLabel;

    public float xSpacing = 20.0f;
    public float ySpacing = 0.0f;

    private MapComponent mapComponent;
    private DataCatalog playerData;

    private SolarSystem system;

    // List with star system scheme elements
    private List<GameObject> schemeElements = null;

    private void Awake()
    {
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.playerData = FindObjectOfType<PlayerController>()?.GetComponent<DataCatalog>();
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

        this.systemLabel.text = this.system.name;
        this.selectedBodyDescriptionLabel.text = "";
        this.selectedBodyNameLabel.text = "";

        // Generate elements from system
        var elements = new List<GameObject>();
        this.schemeElements = elements;

        // Local function which initializes transform of the scheme element
        void InitSchemeBodyTransform(GameObject obj, float x, float y)
        {
            var rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.SetParent(this.schemeRoot.GetComponent<RectTransform>(), worldPositionStays: false);
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
        var starUI = this.InitSchemeBodyProperties(starGameObject, mainBody);
        currentXPos += starGameObject.GetComponent<RectTransform>().rect.width + this.xSpacing;

        var bodies = system.main.children.OfType<StarOrPlanet>()
            .OfType<Body>()
            .Concat(system.belts)
            .OrderBy(b => b is OrbitingBody orbitingBody ? orbitingBody.parameters.semiMajorAxis : ((Belt) b).radius);

        // Iterate star's planets
        foreach (var planet in bodies)
        {
            var planetGameObject = Instantiate(this.schemeBodyPrefab);
            elements.Add(planetGameObject);
            InitSchemeBodyTransform(planetGameObject, currentXPos, currentYPos);
            this.InitSchemeBodyProperties(planetGameObject, planet);

            currentYPos -= planetGameObject.GetComponent<RectTransform>().rect.height + this.ySpacing;

            // Iterate planet's moons
            // Todo what to do with binary systems?
            if (planet is OrbitingBody orbitingBody)
            {
                foreach (var moon in orbitingBody.children.OfType<StarOrPlanet>())
                {
                    var moonGameObject = Instantiate(this.schemeBodyPrefab);
                    elements.Add(moonGameObject);
                    InitSchemeBodyTransform(moonGameObject, currentXPos, currentYPos);
                    this.InitSchemeBodyProperties(moonGameObject, moon);

                    // Right now there is some vertical gap in these prefabs itself
                    currentYPos -= moonGameObject.GetComponent<RectTransform>().rect.height;
                }
            }

            // Add some offset between them
            currentXPos += planetGameObject.GetComponent<RectTransform>().rect.width + this.xSpacing; 
            currentYPos = 0;
        }

        this.SelectBody(starUI);
    }

    // Sets generic body properties in the scheme view, such as...
    // Body's displayed name, appearence, ...
    private StarSystemUIBody InitSchemeBodyProperties(GameObject schemeElement, Body bodyData)
    {
        var bodyComponent = schemeElement.GetComponent<StarSystemUIBody>();
        bodyComponent.bodyName = bodyData.name; // Read the body name here when we have it
        bodyComponent.starSystemUI = this; // It must point back to call functions when clicked
        bodyComponent.actualBody = bodyData;
        bodyComponent.stationIcon.enabled = (bodyData as OrbitingBody)?.children.OfType<Station>().Any() ?? false;
        var bodySpec = this.mapComponent.bodySpecs.GetSpecById(bodyData.specId);

        // Add icon representing the body type, if discovered
        // Otherwise add icon of a question mark
        var knownDataMask = (bodyData is OrbitingBody && this.playerData != null)? this.playerData.GetData(bodyData.bodyRef) : DataMask.All;
        GameObject uiPrefab = this.unknownBodyIconPrefab;

        if (knownDataMask.HasFlag(DataMask.Orbit))
            if (bodySpec.uiPrefab != null)
                uiPrefab = bodySpec.uiPrefab;

        Object.Instantiate(uiPrefab, bodyComponent.iconRoot);

        return bodyComponent;
    }

    // Called when we click on scheme body
    // body - the body which was clicked
    public void SelectBody(StarSystemUIBody body)
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
        this.selectedBodyNameLabel.text = actualBody.name;
        
        var knownDataMask = this.playerData?.GetData(actualBody.bodyRef) ?? DataMask.All;
        //var knownDataStr = new List<string>{"Type: Planet"};
        var knownData = actualBody.GetData(knownDataMask, this.mapComponent.bodySpecs);
        this.selectedBodyDescriptionLabel.text = string.Join("\n", knownData.Select(d => $"{d.name}: {d.entry}{d.units}"));
    }

    #region IUILayer
    public void OnAdded() => this.GenerateSchemeFromSystem(this.mapComponent.selectedSystem ?? this.mapComponent.currentSystem);

    public void OnRemoved() {}

    public void OnDemoted() {}

    public void OnPromoted() {}
    #endregion IUILayer
}
