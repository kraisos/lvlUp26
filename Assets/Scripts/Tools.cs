using UnityEngine;
using System;
using System.Collections.Generic;

public class Tools : MonoBehaviour
{
    [Serializable]
    private class ToolBinding
    {
        public string itemId;
        public GameObject toolObject;
    }

    [Header("References")]
    [SerializeField] private InventoryBarUI inventoryBarUI;
    [SerializeField] private bool autoFindInventoryBarUI = true;

    [Header("Tool Mappings")]
    [SerializeField] private List<ToolBinding> toolBindings = new List<ToolBinding>();

    private void Awake()
    {
        if (inventoryBarUI == null && autoFindInventoryBarUI)
        {
            inventoryBarUI = FindFirstObjectByType<InventoryBarUI>();
        }

        SetAllToolsActive(false);
    }

    private void OnEnable()
    {
        if (inventoryBarUI != null)
        {
            inventoryBarUI.SelectedItemIdChanged += HandleSelectedItemChanged;
            HandleSelectedItemChanged(inventoryBarUI.SelectedItemId);
        }
    }

    private void OnDisable()
    {
        if (inventoryBarUI != null)
        {
            inventoryBarUI.SelectedItemIdChanged -= HandleSelectedItemChanged;
        }
    }

    private void HandleSelectedItemChanged(string selectedItemId)
    {
        SetAllToolsActive(false);

        if (string.IsNullOrWhiteSpace(selectedItemId))
        {
            return;
        }

        for (var i = 0; i < toolBindings.Count; i++)
        {
            var binding = toolBindings[i];
            if (binding == null || binding.toolObject == null)
            {
                continue;
            }

            if (string.Equals(binding.itemId, selectedItemId, StringComparison.OrdinalIgnoreCase))
            {
                binding.toolObject.SetActive(true);
            }
        }
    }

    private void SetAllToolsActive(bool isActive)
    {
        for (var i = 0; i < toolBindings.Count; i++)
        {
            var binding = toolBindings[i];
            if (binding == null || binding.toolObject == null)
            {
                continue;
            }

            binding.toolObject.SetActive(isActive);
        }
    }
}
