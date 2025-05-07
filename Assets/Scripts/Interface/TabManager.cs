using UnityEngine;
using System.Collections.Generic;

public class TabManager : MonoBehaviour
{
    [SerializeField] private Tab defaultTab;
    private List<Tab> tabs = new();
    private Tab currentTab;

    private void Start()
    {
        // Get all Tab components that are children of this TabManager
        tabs.AddRange(GetComponentsInChildren<Tab>());

        // If no default tab is set, use the first tab
        if (defaultTab == null && tabs.Count > 0)
            defaultTab = tabs[0];

        // Initialize all tabs as inactive
        foreach (Tab tab in tabs)
        {
            tab.SetActive(false);
        }

        // Activate the default tab
        if (defaultTab != null)
            OnTabSelected(defaultTab);
    }

    public void OnTabSelected(Tab selectedTab)
    {
        // Deactivate current tab
        currentTab?.SetActive(false);

        // Activate new tab
        selectedTab.SetActive(true);
        currentTab = selectedTab;
    }
}
