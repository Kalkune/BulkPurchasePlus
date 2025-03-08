using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BuyAllButton.Patches
{
    [HarmonyPatch(typeof(GameCanvas))]
    internal class NotificationHandler
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void NotificationHandler_Postfix(GameCanvas __instance, ref bool ___inCooldown)
        {
            if (BulkPurchasePlus.BulkPurchasePlus.notificationSent)
            {
                ___inCooldown = false;
                BulkPurchasePlus.BulkPurchasePlus.notificationSent = false;
                string text = "`";
                switch (BulkPurchasePlus.BulkPurchasePlus.notificationMessage)
                {
                    case "ThresholdToggle":
                        switch (BulkPurchasePlus.BulkPurchasePlus.currentMode)
                        {
                            case 1:
                                text += "Threshold: Shelves Filled";
                                break;
                            case 2:
                                text += "Threshold: " + BulkPurchasePlus.BulkPurchasePlus.HardThreshold.Value + " On Shelf";
                                break;
                            case 3:
                                text += "Threshold: Shelf Mixed";
                                break;
                            case 4:
                                text += "Threshold: " + BulkPurchasePlus.BulkPurchasePlus.StorageThreshold.Value + " Product In Storage";
                                break;
                            case 5:
                                text += "Threshold: " + BulkPurchasePlus.BulkPurchasePlus.StorageBoxThreshold.Value + " Boxes In Storage";
                                break;
                            case 6:
                                text += "Threshold: Storage Mixed";
                                break;
                            case 7:
                                text += "Threshold: Storage Filled";
                                break;
                        }
                        break;
                    case "ProductBlacklist":
                        if (BulkPurchasePlus.BulkPurchasePlus.productToggledBlacklist)
                            text += "Blacklisted: " + BulkPurchasePlus.BulkPurchasePlus.productToggled;
                        else if (!BulkPurchasePlus.BulkPurchasePlus.productToggledBlacklist)

                            text += "Un-Blacklisted: " + BulkPurchasePlus.BulkPurchasePlus.productToggled;
                        break;
                    case "BuyMaxToggle":
                        if (BulkPurchasePlus.BulkPurchasePlus.buyMaxToggle)
                            text += "Buy Max Enabled";
                        else if (!BulkPurchasePlus.BulkPurchasePlus.buyMaxToggle)
                            text += "Buy Max Disabled";
                        break;
                }
                __instance.CreateCanvasNotification(text);
            }
        }
        [HarmonyPatch(typeof(LocalizationManager))]
        internal class LocalizationHandler
        {
            [HarmonyPatch("GetLocalizationString")]
            [HarmonyPrefix]
            public static bool noLocalization_Prefix(ref string key, ref string __result)
            {
                if (key[0] == '`')
                {
                    __result = key.Substring(1);
                    return false;
                }
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(PlayerNetwork), nameof(PlayerNetwork.OnStartClient))]
    public class AddButton
    {
        public static bool Prefix()
        {
            // Find the Buttons_Bar GameObject
            GameObject buttonsBar = GameObject.Find("Buttons_Bar");

            if (buttonsBar == null)
            {
                return true;
            }

            // Create the "Add All to Cart" button if it doesn't exist
            if (buttonsBar.transform.Find("AddAllToCartButton") == null)
            {
                GameObject addAllButton = CreateButton(buttonsBar, "AddAllToCartButton", -450, 110); // Full width
                AddButtonEvents(addAllButton.GetComponent<Button>(), addAllButton.GetComponent<Image>(), OnAddAllToCartButtonClick);
            }

            // Create the "Buy Max" button if it doesn't exist
            if (buttonsBar.transform.Find("BuyMaxButton") == null)
            {
                GameObject buyMaxButton = CreateButton(buttonsBar, "BuyMaxButton", 425, 110); // Shifted 800 units to the right
                AddButtonEvents(buyMaxButton.GetComponent<Button>(), buyMaxButton.GetComponent<Image>(), OnBuyMaxButtonClick);
            }

            // Create the new button if it doesn't exist
            if (buttonsBar.transform.Find("NeedsOnlyButton") == null)
            {
                // Position this button 50px to the right of the "AddAllToCartButton"
                GameObject newButton = CreateButton(buttonsBar, "NeedsOnlyButton", -325, 55); // Half width, 50px right
                AddButtonEvents(newButton.GetComponent<Button>(), newButton.GetComponent<Image>(), OnNeedsOnlyButtonClick);
            }

            return true;
        }

        private static GameObject CreateButton(GameObject parent, string name, float xOffset, float width)
        {
            // Create the button GameObject
            GameObject buttonObject = new GameObject(name);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            Button buttonComponent = buttonObject.AddComponent<Button>();
            Image buttonImage = buttonObject.AddComponent<Image>();

            // Set the button's parent to Buttons_Bar
            buttonObject.transform.SetParent(parent.transform, false);

            // Set up RectTransform properties
            rectTransform.sizeDelta = new Vector2(width, 35); // Adjust width here
            rectTransform.anchoredPosition = new Vector2(xOffset, 612);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Create and configure the text component
            GameObject textObject = new GameObject("ButtonText");
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
            Text textComponent = textObject.AddComponent<Text>();

            textRectTransform.sizeDelta = rectTransform.sizeDelta;
            textRectTransform.anchoredPosition = Vector2.zero;

            textComponent.text = name == "AddAllToCartButton" ? "Add All to Cart" :
                                  name == "BuyMaxButton" ? "Toggle Buy Max" : "Needs Only Button";
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.black;
            textComponent.fontStyle = FontStyle.Bold;

            return buttonObject;
        }

        private static void AddButtonEvents(Button button, Image buttonImage, UnityEngine.Events.UnityAction onClickAction)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

            // Hover enter event
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            pointerEnter.callback.AddListener((data) => OnHoverEnter(buttonImage));
            trigger.triggers.Add(pointerEnter);

            // Hover exit event
            EventTrigger.Entry pointerExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            pointerExit.callback.AddListener((data) => OnHoverExit(buttonImage));
            trigger.triggers.Add(pointerExit);

            // Click event (only triggers if hovered)
            button.onClick.AddListener(() =>
            {
                if (buttonImage.color != Color.white)
                {
                    onClickAction.Invoke();
                }
            });
        }

        private static void OnHoverEnter(Image buttonImage)
        {
            buttonImage.color = new Color(5f / 255f, 133f / 255f, 208f / 255f); // Light blue hover color
        }

        private static void OnHoverExit(Image buttonImage)
        {
            buttonImage.color = Color.white; // Revert color when not hovering
        }

        private static void OnAddAllToCartButtonClick()
        {
            ProductListing productListing = GameObject.FindFirstObjectByType<ProductListing>();
            ManagerBlackboard managerBlackboard = GameObject.FindFirstObjectByType<ManagerBlackboard>();

            if (productListing == null || managerBlackboard == null) return;

            foreach (var productPrefab in productListing.productPrefabs)
            {
                var productComponent = productPrefab.GetComponent<Data_Product>();
                if (productComponent != null && productListing.unlockedProductTiers[productComponent.productTier])
                {
                    float boxPrice = productComponent.basePricePerUnit * productComponent.maxItemsPerBox;
                    boxPrice *= productListing.tierInflation[productComponent.productTier];
                    float roundedBoxPrice = Mathf.Round(boxPrice * 100f) / 100f;
                    managerBlackboard.AddShoppingListProduct(productComponent.productID, roundedBoxPrice);
                }
            }
        }

        private static void OnBuyMaxButtonClick()
        {
            ManagerBlackboard managerBlackboard = GameObject.FindFirstObjectByType<ManagerBlackboard>();

            if (managerBlackboard == null) return;

            BulkPurchasePlus.BulkPurchasePlus.buyMaxToggle = !BulkPurchasePlus.BulkPurchasePlus.buyMaxToggle;
            BulkPurchasePlus.BulkPurchasePlus.notificationMessage = "BuyMaxToggle";
            BulkPurchasePlus.BulkPurchasePlus.notificationSent = true;
        }

        private static void OnNeedsOnlyButtonClick()
        {
            ProductListing productListing = GameObject.FindFirstObjectByType<ProductListing>();
            ManagerBlackboard managerBlackboard = GameObject.FindFirstObjectByType<ManagerBlackboard>();

            if (productListing == null || managerBlackboard == null) return;

            // Define your threshold for product existence

            foreach (var productPrefab in productListing.productPrefabs)
            {
                var productComponent = productPrefab.GetComponent<Data_Product>();
                if (productComponent != null && productListing.unlockedProductTiers[productComponent.productTier])
                {
                    int productID = productComponent.productID;
                    bool order = false;
                    // Get the count of existing products
                    int[] productExistences = managerBlackboard.GetProductsExistences(productID);
                    int totalExistence = 0;
                    foreach (int count in productExistences)
                    {
                        totalExistence += count;
                    }

                    if (BulkPurchasePlus.BulkPurchasePlus.currentShoppingList.Contains(productComponent.name))
                        totalExistence += BulkPurchasePlus.BulkPurchasePlus.currentShoppingList.FindAll(s => s.Equals(productComponent.name)).Count * productComponent.maxItemsPerBox;

                    order = CheckOrderAviablility(totalExistence, productID, BulkPurchasePlus.BulkPurchasePlus.currentMode, productComponent);

                    if (BulkPurchasePlus.BulkPurchasePlus.buyMaxToggle && order)
                    {
                        int buyMaxMaybe=0;
                        if (BulkPurchasePlus.BulkPurchasePlus.currentMode < 7)
                            buyMaxMaybe = BulkPurchasePlus.BulkPurchasePlus.somethingsomething(NPC_Manager.Instance, productID, productComponent, false);
                        else if (BulkPurchasePlus.BulkPurchasePlus.currentMode == 7)
                            buyMaxMaybe = BulkPurchasePlus.BulkPurchasePlus.somethingsomething(NPC_Manager.Instance, productID, productComponent, true);
                        else
                            Debug.LogError("Couldn't choose a mode");
                        buyMaxMaybe = buyMaxMaybe - totalExistence;
                        float boxPrice = productComponent.basePricePerUnit * productComponent.maxItemsPerBox;
                        boxPrice *= productListing.tierInflation[productComponent.productTier];
                        float roundedBoxPrice = Mathf.Round(boxPrice * 100f) / 100f;
                        for (int i = 0; i < Math.Ceiling((float)buyMaxMaybe/productComponent.maxItemsPerBox); i++)
                            managerBlackboard.AddShoppingListProduct(productID, roundedBoxPrice);
                    }
                    else if (order)
                    {
                        float boxPrice = productComponent.basePricePerUnit * productComponent.maxItemsPerBox;
                        boxPrice *= productListing.tierInflation[productComponent.productTier];
                        float roundedBoxPrice = Mathf.Round(boxPrice * 100f) / 100f;
                        managerBlackboard.AddShoppingListProduct(productID, roundedBoxPrice);
                    }

                }
            }
        }
        public static bool CheckOrderAviablility(int totalExistence, int productID, int orderNumber, Data_Product productComponent)
        {
            bool order = false;
            if (BulkPurchasePlus.BulkPurchasePlus.productBlacklist.Contains(productComponent.name))
                return order;
            switch (orderNumber)
            {
                case 1://Shelves Filled
                    if (totalExistence < BulkPurchasePlus.BulkPurchasePlus.somethingsomething(NPC_Manager.Instance, productID, productComponent, false))
                        order = true;
                    break;
                case 2://Items on Shelf
                    if (totalExistence < BulkPurchasePlus.BulkPurchasePlus.threshold)
                        order = true;
                    break;
                case 3://Shelves Mixed
                    if (CheckOrderAviablility(totalExistence, productID, 1, productComponent) && CheckOrderAviablility(totalExistence, productID, 2, productComponent))
                        order = true;
                    break;
                case 4://Product in Storage
                    if (totalExistence < BulkPurchasePlus.BulkPurchasePlus.somethingsomething(NPC_Manager.Instance, productID, productComponent, false) + BulkPurchasePlus.BulkPurchasePlus.StorageThreshold.Value)
                        order = true;
                    break;
                case 5://Boxes in Storage
                    if (totalExistence < BulkPurchasePlus.BulkPurchasePlus.somethingsomething(NPC_Manager.Instance, productID, productComponent, false) + ((BulkPurchasePlus.BulkPurchasePlus.StorageBoxThreshold.Value * productComponent.maxItemsPerBox)-productComponent.maxItemsPerBox + 1))
                        order = true;
                    break;
                case 6://Storage Mixed
                    if (CheckOrderAviablility(totalExistence, productID, 4, productComponent) && CheckOrderAviablility(totalExistence, productID, 5, productComponent))
                        order = true;
                    break;
                case 7://Storage Filled
                    if (totalExistence < (BulkPurchasePlus.BulkPurchasePlus.somethingsomething(NPC_Manager.Instance, productID, productComponent, true)-productComponent.maxItemsPerBox+1))
                        order = true;
                    break;
            }
            return order;
        }
    }
}