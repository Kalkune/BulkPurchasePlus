using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker;
using Mirror;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace BulkPurchasePlus
{
    [BepInPlugin("com.Kalkune.BulkPurchasePlus", "BulkPurchasePlus", "1.0.2")]
    public class BulkPurchasePlus : BaseUnityPlugin
    {

        internal static Harmony Harmony;
		public static ConfigEntry<int> HardThreshold { get; set; }
		public static ConfigEntry<int> StorageThreshold { get; set; }
		public static ConfigEntry<int> StorageBoxThreshold { get; set; }
		public static ConfigEntry<KeyboardShortcut> KeyboardShortcutDoublePrice { get; set; }
		public static ConfigEntry<KeyboardShortcut> KeyboardShortcutBlacklist { get; set; }
		public static ConfigEntry<string> Help { get; set; }
		public static int threshold;
		public static int currentMode = 1;
		public static bool notificationSent = false;
		public static string notificationMessage;
		public static string productToggled;
		public static bool productToggledBlacklist;
		public static string productToggledInternalName;
		public static bool buyMaxToggle;
		public static int buyMaxCycleCount;
		public static List<string> productBlacklist = new List<string>();
		public static List<string> currentShoppingList = new List<string>();
		public void Awake()
		{
			HardThreshold = Config.Bind<int>("Threshold", "Hard Threshold Cap", 50, "Products with quantity above this number won't be ordered with Needs Only if on alternate modes.");
			StorageThreshold = Config.Bind<int>("Threshold", "Storage Threshold Cap", 30, "Products with quantity above this number won't be ordered with Product Storage Mode.");
			StorageBoxThreshold = Config.Bind<int>("Threshold", "Storage Box Threshold Cap", 4, "Products with box quantity above this number won't be ordered with Box Storage Mode.");
			KeyboardShortcutDoublePrice = Config.Bind<KeyboardShortcut>("Threshold", "Toggle Threshold Keybind", new KeyboardShortcut((KeyCode)114, Array.Empty<KeyCode>()), "Keybind to toggle through modes");
			Help = Config.Bind<string>("Blacklist", "Blacklist List", "", "Current list of product blacklisted");
			KeyboardShortcutBlacklist = Config.Bind<KeyboardShortcut>("Blacklist", "Toggle Blacklist Keybind", new KeyboardShortcut((KeyCode)105, Array.Empty<KeyCode>()), "Keybind to toggle products blacklist status");
			threshold = HardThreshold.Value;
			Harmony = new("com.Kalkune.BulkPurchasePlus");
            Harmony.PatchAll();
			productBlacklist = Help.Value.Split(',').ToList();
			Hook ManagerBlackboardAddShoppingListProduct = new Hook(typeof(ManagerBlackboard).GetMethod("AddShoppingListProduct", BindingFlags.Instance | BindingFlags.Public), typeof(BulkPurchasePlus).GetMethod("ManagerBlackboardAddShoppingListProduct"));
			Hook ManagerBlackboardRemoveShoppingListProduct = new Hook(typeof(ManagerBlackboard).GetMethod("RemoveShoppingListProduct", BindingFlags.Instance | BindingFlags.Public), typeof(BulkPurchasePlus).GetMethod("ManagerBlackboardRemoveShoppingListProduct"));
			Hook ManagerBlackboardRemoveAllShoppingList = new Hook(typeof(ManagerBlackboard).GetMethod("RemoveAllShoppingList", BindingFlags.Instance | BindingFlags.Public), typeof(BulkPurchasePlus).GetMethod("ManagerBlackboardRemoveAllShoppingList"));
		}
		public static void ManagerBlackboardAddShoppingListProduct(Action<ManagerBlackboard , int , float> orig, ManagerBlackboard self, int productID, float boxPrice)
        {
			orig(self, productID, boxPrice);

			ProductListing component = GameObject.FindFirstObjectByType<ProductListing>();
			GameObject gameObject2 = component.productPrefabs[productID];
			currentShoppingList.Add(gameObject2.name);
			//Debug.LogError("Ordered: " + gameObject2.name);
		}
		public static void ManagerBlackboardRemoveShoppingListProduct(Action<ManagerBlackboard, int> orig, ManagerBlackboard self, int indexToRemove)
		{
			//Debug.LogError("Removing: " + currentShoppingList[indexToRemove]);
			currentShoppingList.RemoveAt(indexToRemove);


			orig(self, indexToRemove);
		}
		public static void ManagerBlackboardRemoveAllShoppingList(Action<ManagerBlackboard> orig, ManagerBlackboard self)
		{
			//Debug.LogError("Clearing Current Shopping List");
			currentShoppingList.Clear();
			orig(self);
		}
		public void Update()
        {
			KeyboardShortcut value = KeyboardShortcutDoublePrice.Value;
			if (value.IsDown())
			{
				if (currentMode >= 7)
					currentMode = 1;
				else
					currentMode++;
				notificationMessage = "ThresholdToggle";
				notificationSent = true;
				return;
			}
			PlayerNetwork pN = GameObject.FindFirstObjectByType<PlayerNetwork>();

			if (pN && pN.gameCanvasProductOBJ && pN.gameCanvasProductOBJ.activeSelf)
			{
				KeyboardShortcut value2 = KeyboardShortcutBlacklist.Value;
				if (value2.IsDown())
				{
					productToggled = pN.gameCanvasProductOBJ.transform.Find("Container/ProductName").GetComponent<TextMeshProUGUI>().text;
					productToggledInternalName = pN.gameCanvasProductOBJ.transform.Find("Container/ProductImage").GetComponent<Image>().mainTexture.name;
					if (!productBlacklist.Contains(productToggledInternalName))
					{
						productBlacklist.Add(productToggledInternalName);
						productToggledBlacklist = true;
					}
					else if (productBlacklist.Contains(productToggledInternalName))
					{
						productBlacklist.Remove(productToggledInternalName);
						productToggledBlacklist = false;
					}
					notificationMessage = "ProductBlacklist";
					notificationSent = true;
					Help.Value = string.Join(",", productBlacklist);
					//Debug.LogError(productToggled);
				}
			}
		}

        public static int somethingsomething(NPC_Manager __instance, int productId, Data_Product productComponent, bool includeStorage)
        {
            if (__instance.shelvesOBJ.transform.childCount == 0)
            {
                return 0;
            }

            int productCount = 0;
			int productQuantity = 0;

			for (int i = 0; i < __instance.shelvesOBJ.transform.childCount; i++)
            {
                int[] productInfoArray = __instance.shelvesOBJ.transform.GetChild(i).GetComponent<Data_Container>().productInfoArray;
				int num = productInfoArray.Length / 2;
                for (int j = 0; j < num; j++)
				{

					int storageProductId = productInfoArray[j * 2];
					//int productQuantity = productInfoArray[j * 2 + 1];

					if (storageProductId == productId)
					{
						productCount++;
					}
					Data_Container shelfinfo = __instance.shelvesOBJ.transform.GetChild(i).GetComponent<Data_Container>();
					if (!shelfinfo.productlistComponent)
						break;
					GameObject gameObject = shelfinfo.productlistComponent.productPrefabs[productId];
					Vector3 size = gameObject.GetComponent<BoxCollider>().size;

					bool isStackable = gameObject.GetComponent<Data_Product>().isStackable;
					int num1 = Mathf.FloorToInt(shelfinfo.shelfLength / (size.x * 1.1f));
					num1 = Mathf.Clamp(num1, 1, 100);
					int num2 = Mathf.FloorToInt(shelfinfo.shelfWidth / (size.z * 1.1f));
					num2 = Mathf.Clamp(num2, 1, 100);
					int num3 = num1 * num2;
					if (isStackable)
					{
						int num4 = Mathf.FloorToInt(shelfinfo.shelfHeight / (size.y * 1.1f));
						num4 = Mathf.Clamp(num4, 1, 100);
						num3 = num1 * num2 * num4;
					}
					productQuantity += num3 * productCount;
					productCount = 0;

				}
			}
			if (__instance.storageOBJ.transform.childCount == 0)
			{
				Debug.LogWarning("Stopped cause some reason I don't really know");
				return productQuantity;
			}
			if (!includeStorage)
			{
				for (int i = 0; i < __instance.storageOBJ.transform.childCount; i++)
				{
					if (__instance.storageOBJ.transform.GetChild(i).GetComponent<Data_Container>().name != "5_StorageShelf(Clone)")
						break;
					int[] productInfoArray = __instance.storageOBJ.transform.GetChild(i).GetComponent<Data_Container>().productInfoArray;
					int num = productInfoArray.Length / 2;
					for (int j = 0; j < num; j++)
					{

						int storageProductId = productInfoArray[j * 2];
						//int productQuantity = productInfoArray[j * 2 + 1];

						//-------Debug.LogError(storageProductId);
						if (storageProductId == productId)
						{
							productCount++;
						}
						productQuantity += productComponent.maxItemsPerBox * productCount;
						productCount = 0;

					}
				}
			}
			//Debug.LogWarning("Max Quantity for Product " + productComponent.name + ":" + productQuantity);
			return productQuantity;
        }
	}
}