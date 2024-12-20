using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterSMT.Patches {
    [HarmonyPatch(typeof(GameCanvas))]
    internal class NotificationHandler {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void NotificationHandler_Postfix(GameCanvas __instance, ref bool ___inCooldown) {
            if (!BulkPurchasePlus.BulkPurchasePlus.notificationSent) {
                return;
            }

            ___inCooldown = false;
            BulkPurchasePlus.BulkPurchasePlus.notificationSent = false;

            string notificationText = GenerateNotificationText(BulkPurchasePlus.BulkPurchasePlus.currentMode);
            __instance.CreateCanvasNotification(notificationText);
        }

        private static string GenerateNotificationText(int mode) {
            return mode switch {
                1 => "`Threshold: Shelves Filled",
                2 => $"`Threshold: {BulkPurchasePlus.BulkPurchasePlus.HardThreshold.Value} On Shelf",
                3 => "`Threshold: Shelf Mixed",
                4 => $"`Threshold: {BulkPurchasePlus.BulkPurchasePlus.StorageThreshold.Value} Product In Storage",
                5 => $"`Threshold: {BulkPurchasePlus.BulkPurchasePlus.StorageBoxThreshold.Value} Boxes In Storage",
                6 => "`Threshold: Storage Mixed",
                _ => "`Threshold: Unknown",
            };
        }
    }

    [HarmonyPatch(typeof(LocalizationManager))]
    internal class LocalizationHandler {
        [HarmonyPatch("GetLocalizationString")]
        [HarmonyPrefix]
        public static bool HandleCustomLocalization(ref string key, ref string __result) {
            if (key.StartsWith("`")) {
                __result = key[1..];
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerNetwork), nameof(PlayerNetwork.OnStartClient))]
    public class ButtonManager {
        private const float ButtonWidth = 110;
        private const float ButtonHeight = 35;

        public static bool Prefix() {
            GameObject buttonsBar = GameObject.Find("Buttons_Bar");
            if (buttonsBar == null) {
                return true;
            }

            CreateButtonIfMissing(buttonsBar, "AddAllToCartButton", -450, ButtonWidth, "Add All to Cart", OnAddAllToCartButtonClick);
            CreateButtonIfMissing(buttonsBar, "RemoveAllFromCartButton", 425, ButtonWidth, "Remove All from Cart", OnRemoveAllFromCartButtonClick);
            CreateButtonIfMissing(buttonsBar, "NeedsOnlyButton", -325, ButtonWidth, "Needs Only", OnNeedsOnlyButtonClick);

            return true;
        }

        private static void CreateButtonIfMissing(GameObject parent, string name, float xOffset, float width, string text, UnityEngine.Events.UnityAction onClickAction) {
            if (parent.transform.Find(name) != null) {
                return;
            }

            GameObject button = UIUtils.CreateButton(parent, name, xOffset, width, ButtonHeight, text);
            UIUtils.AddButtonEvents(button.GetComponent<Button>(), button.GetComponent<Image>(), onClickAction);
        }

        private static void OnAddAllToCartButtonClick() {
            ProductListing productListing = Object.FindFirstObjectByType<ProductListing>();
            ManagerBlackboard managerBlackboard = Object.FindFirstObjectByType<ManagerBlackboard>();

            if (productListing == null || managerBlackboard == null) {
                return;
            }

            foreach (GameObject productPrefab in productListing.productPrefabs) {
                Data_Product product = productPrefab.GetComponent<Data_Product>();
                if (product != null && productListing.unlockedProductTiers[product.productTier]) {
                    float roundedBoxPrice = CalculateBoxPrice(product, productListing.tierInflation[product.productTier]);
                    managerBlackboard.AddShoppingListProduct(product.productID, roundedBoxPrice);
                }
            }
        }

        private static void OnRemoveAllFromCartButtonClick() {
            ManagerBlackboard managerBlackboard = Object.FindFirstObjectByType<ManagerBlackboard>();
            if (managerBlackboard == null) {
                return;
            }

            for (int i = managerBlackboard.shoppingListParent.transform.childCount - 1; i >= 0; i--) {
                managerBlackboard.RemoveShoppingListProduct(i);
            }
        }

        private static void OnNeedsOnlyButtonClick() {
            ProductListing productListing = Object.FindFirstObjectByType<ProductListing>();
            ManagerBlackboard managerBlackboard = Object.FindFirstObjectByType<ManagerBlackboard>();

            if (productListing == null || managerBlackboard == null) {
                return;
            }

            foreach (GameObject productPrefab in productListing.productPrefabs) {
                Data_Product product = productPrefab.GetComponent<Data_Product>();
                if (product == null || !productListing.unlockedProductTiers[product.productTier]) {
                    continue;
                }

                int totalExistence = managerBlackboard.GetProductsExistences(product.productID).Sum();
                if (ShouldOrderProduct(totalExistence, product.productID, BulkPurchasePlus.BulkPurchasePlus.currentMode, product)) {
                    float roundedBoxPrice = CalculateBoxPrice(product, productListing.tierInflation[product.productTier]);
                    managerBlackboard.AddShoppingListProduct(product.productID, roundedBoxPrice);
                }
            }
        }

        private static bool ShouldOrderProduct(int totalExistence, int productID, int mode, Data_Product product) {
            return mode switch {
                1 => totalExistence < BulkPurchasePlus.BulkPurchasePlus.CalculateThreshold(NPC_Manager.Instance, productID),
                2 => totalExistence < BulkPurchasePlus.BulkPurchasePlus.HardThreshold.Value,
                3 => ShouldOrderProduct(totalExistence, productID, 1, product) && ShouldOrderProduct(totalExistence, productID, 2, product),
                4 => totalExistence < BulkPurchasePlus.BulkPurchasePlus.CalculateThreshold(NPC_Manager.Instance, productID) + BulkPurchasePlus.BulkPurchasePlus.StorageThreshold.Value,
                5 => totalExistence < BulkPurchasePlus.BulkPurchasePlus.CalculateThreshold(NPC_Manager.Instance, productID) + BulkPurchasePlus.BulkPurchasePlus.StorageBoxThreshold.Value * product.maxItemsPerBox,
                6 => ShouldOrderProduct(totalExistence, productID, 4, product) && ShouldOrderProduct(totalExistence, productID, 5, product),
                _ => false,
            };
        }

        private static float CalculateBoxPrice(Data_Product product, float inflationRate) {
            float boxPrice = product.basePricePerUnit * product.maxItemsPerBox;
            return Mathf.Round(boxPrice * inflationRate * 100f) / 100f;
        }
    }

    internal static class UIUtils {
        public static GameObject CreateButton(GameObject parent, string name, float xOffset, float width, float height, string buttonText) {
            GameObject buttonObject = new(name);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            _ = buttonObject.AddComponent<Button>();
            _ = buttonObject.AddComponent<Image>();

            buttonObject.transform.SetParent(parent.transform, false);

            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = new Vector2(xOffset, 612);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            GameObject textObject = new("ButtonText");
            textObject.transform.SetParent(buttonObject.transform, false);

            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = buttonText;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.black;
            textComponent.fontStyle = FontStyle.Bold;

            return buttonObject;
        }

        public static void AddButtonEvents(Button button, Image buttonImage, UnityEngine.Events.UnityAction onClickAction) {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

            AddTriggerEvent(trigger, EventTriggerType.PointerEnter, () => buttonImage.color = new Color(5f / 255f, 133f / 255f, 208f / 255f));
            AddTriggerEvent(trigger, EventTriggerType.PointerExit, () => buttonImage.color = Color.white);

            button.onClick.AddListener(() => {
                if (buttonImage.color != Color.white) {
                    onClickAction.Invoke();
                }
            });
        }

        private static void AddTriggerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction action) {
            EventTrigger.Entry entry = new() { eventID = eventType };
            entry.callback.AddListener((data) => action.Invoke());
            trigger.triggers.Add(entry);
        }
    }
}
