using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HutongGames.PlayMaker;
using Mirror;
using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace BulkPurchasePlus
{
    [BepInPlugin("com.Kalkune.BulkPurchasePlus", "BulkPurchasePlus", "1.0.0")]
    public class BulkPurchasePlus : BaseUnityPlugin
    {

        internal static Harmony Harmony;
		public static ConfigEntry<int> HardThreshold { get; set; }
		public static ConfigEntry<KeyboardShortcut> KeyboardShortcutDoublePrice { get; set; }
		public static int threshold;
		public static int currentMode = 1;
		public static bool notificationSent = false;
		public static string notificationMessage;
		public void Awake()
		{
			HardThreshold = Config.Bind<int>("Threshold", "Hard Threshold Cap", 50, "Products with quantity above this number won't be ordered with Needs Only if on alternate modes.");
			KeyboardShortcutDoublePrice = Config.Bind<KeyboardShortcut>("Threshold", "Toggle Threshold Keybind", new KeyboardShortcut((KeyCode)114, Array.Empty<KeyCode>()), "");
			threshold = HardThreshold.Value;
			Harmony = new("com.Kalkune.BulkPurchasePlus");
            Harmony.PatchAll();
		}
		public void Update()
        {
			KeyboardShortcut value = KeyboardShortcutDoublePrice.Value;
			if (value.IsDown())
			{
				if (currentMode == 1 || currentMode == 2)
					currentMode++;
				else if (currentMode == 3)
					currentMode = 1;
				else
                {
					Debug.LogError("Unknown Mode, setting to 1");
					currentMode = 1;
                }
				notificationMessage = "ThresholdToggle";
				notificationSent = true;
				return;
			}
		}

        public static int somethingsomething(NPC_Manager __instance, int productId)
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
			return productQuantity;
        }
	}
}