using UnityEngine;
using UnityEngine.UI;

public class Tab : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Button button;
    [SerializeField] private Image tabImage;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new(0.7f, 0.7f, 0.7f, 1f);

    private TabManager tabManager;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (tabImage == null)
            tabImage = GetComponent<Image>();

        button.onClick.AddListener(OnTabClicked);
    }

    private void Start()
    {
        tabManager = GetComponentInParent<TabManager>();
        if (tabManager == null)
            Debug.LogError("Tab must be a child of a TabManager!");
    }

    public void SetActive(bool active)
    {
        content?.SetActive(active);

        if (tabImage != null)
            tabImage.color = active ? activeColor : inactiveColor;
    }

    private void OnTabClicked()
    {
        tabManager?.OnTabSelected(this);
    }
}
