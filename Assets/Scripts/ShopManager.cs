using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;
    
    [SerializeField] Text MedicineCountText;
    [SerializeField] Text AmmoCountText;
    [SerializeField] Text KnifeCountText;

    string userKey;
    int currentCoin;
    Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shinguexam2601039-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadUserData();
    }

    public void LoadUserData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보를 찾을 수 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "유저 정보 불러오기 실패";
                });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
                string inventoryJson = snapshot.Child("Inventory").Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "유저 정보 불러오기 완료";
                });
            }
        });
    }

    void RefreshUI()
    {
        CoinText.text = "Coin : " + currentCoin;
        UpdateInventoryDisplay();
    }

    void UpdateInventoryDisplay()
    {
        if (MedicineCountText != null)
            MedicineCountText.text = "Medicine : " + GetItemCount("Medicine");
        if (AmmoCountText != null)
            AmmoCountText.text = "Ammo : " + GetItemCount("Ammo");
        if (KnifeCountText != null)
            KnifeCountText.text = "Knife : " + GetItemCount("Knife");
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
            return inventory[itemName];
        return 0;
    }

    public void OnClickBuyMedicine()
    {
        BuyItem("Medicine", 30);
    }

    public void OnClickBuyAmmo()
    {
        BuyItem("Ammo", 50);
    }

    public void OnClickBuyKnife()
    {
        BuyItem("Knife", 250);
    }

    void BuyItem(string itemName, int price)
    {
        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;
        
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory.Add(itemName, 1);
        }

        UpdateUserData();
        MessageText.text = itemName + " 구매 완료! (" + price + " Coin 사용됨)";
    }

    void UpdateUserData()
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(new Dictionary<string, object>
        {
            { "Coin", currentCoin },
            { "Inventory", inventoryJson }
        }).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "데이터 저장 실패";
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                RefreshUI();
            });
        });
    }

    void Update()
    {

    }
}
