using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelListFiller : MonoBehaviour
{
    [SerializeField] Sprite[] levelSprites;
    [SerializeField] GameObject levelElementPrefab;
    [SerializeField] string incompleteLevelSignName;
    [SerializeField] string completedLevelSignName;
    [Space]
    [SerializeField] TextAsset levelTreeConfig;

    private float elementHeight;

    private LevelTree levelTree;
    private int publishedLevelCount;
    private int completedLevelCount;
    private List<GameObject> levelElements;
    private RectTransform incompletePrefabRect, completedPrefabRect;

    private int availableLevels => Mathf.Min(completedLevelCount + 1, publishedLevelCount);

    private void CountLevels()
    {
        for (int i = 0; i < levelTree.levels.Length; i++)
        {
            if (!levelTree.levels[i].published)
                continue;
            publishedLevelCount++;
            if (levelTree.levels[i].completed)
                completedLevelCount++;
        }
    }
    private void ClearPrevious ()
    {
        for (int i = levelElements.Count - 1; i >= 0; i--)
            Destroy(levelElements[i]);
        levelElements.Clear();
    }
    private void CreateAccordingElements ()
    {

        for (int i = 0; i < availableLevels; i++)
        {
            GameObject go = new GameObject($"LevelElement ({i})");
            levelElements.Add(go);
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.SetParent(transform);
            rectTransform.localPosition = Vector3.up * elementHeight * i;
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = levelElementPrefab.GetComponent<RectTransform>().sizeDelta;
            go.AddComponent<Image>().sprite = levelSprites[i];

            RectTransform prefabRect = i == completedLevelCount ? incompletePrefabRect : completedPrefabRect;

            RectTransform signRect = new GameObject("Sign").AddComponent<RectTransform>();
            signRect.SetParent(go.transform);
            signRect.transform.localScale = Vector3.one;
            signRect.localPosition = prefabRect.localPosition;
            signRect.sizeDelta = prefabRect.sizeDelta;
            Image signImage = signRect.gameObject.AddComponent<Image>();
            signImage.sprite = prefabRect.gameObject.GetComponent<Image>().sprite;
        }
        
    }
    private void UpdateFill ()
    {
        CountLevels();
        ClearPrevious();
        CreateAccordingElements();

    }

    private void Start()
    {
        levelTree = LevelTree.ExtractFromText(levelTreeConfig.text);
        levelElements = new List<GameObject>();

        elementHeight = levelElementPrefab.GetComponent<RectTransform>().sizeDelta.y;

        for (int j = 0; j < levelElementPrefab.transform.childCount; j++)
        {
            if (levelElementPrefab.transform.GetChild(j).name == incompleteLevelSignName)
                incompletePrefabRect = levelElementPrefab.transform.GetChild(j).GetComponent<RectTransform>();
            else if (levelElementPrefab.transform.GetChild(j).name == completedLevelSignName)
                completedPrefabRect = levelElementPrefab.transform.GetChild(j).GetComponent<RectTransform>();
        }
        UpdateFill();
    }
}