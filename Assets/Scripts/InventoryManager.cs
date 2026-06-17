using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text MedicineCountText;
    [SerializeField] Text AmmoCountText;
    [SerializeField] Text KnifeCountText;
    [SerializeField] Text MessageText;

    string userKey;
    Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shinguexam2601039-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadInventory();
    }

    void LoadInventory()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보를 찾을 수 없습니다.";
            Debug.LogError("UserKey가 없습니다!");
            return;
        }

        Debug.Log("인벤토리 로드 시작 - UserKey: " + userKey);

        reference.Child("UserInfo").Child(userKey).Child("Inventory").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 불러오기 실패";
                    Debug.LogError("Firebase 읽기 실패: " + task.Exception);
                });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 정보가 없습니다.";
                    });
                    Debug.LogError("Inventory 값이 null입니다!");
                    return;
                }

                try
                {
                    string inventoryJson = snapshot.Value.ToString();
                    Debug.Log("받은 Inventory JSON: " + inventoryJson);

                    inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                    if (inventory == null)
                    {
                        Debug.LogError("JSON 파싱 후 inventory가 null입니다!");
                        inventory = new Dictionary<string, int>();
                        inventory["Medicine"] = 0;
                        inventory["Ammo"] = 0;
                        inventory["Knife"] = 0;
                    }

                    dispatcher.Enqueue(() =>
                    {
                        RefreshUI();
                        MessageText.text = "인벤토리 불러오기 완료";
                        Debug.Log("인벤토리 로드 성공");
                    });
                }
                catch (System.Exception e)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "데이터 파싱 오류";
                        Debug.LogError("JSON 파싱 오류: " + e.Message);
                    });
                }
            }
        });
    }

    // ✅ 추가: UI 새로고침 공개 메서드
    public void RefreshInventoryUI()
    {
        Debug.Log("인벤토리 UI 새로고침 요청");
        LoadInventory();
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
        {
            int count = inventory[itemName];
            Debug.Log(itemName + " 개수: " + count);
            return count;
        }
        Debug.LogWarning(itemName + "이(가) 인벤토리에 없습니다! 사용 가능한 키: " + string.Join(", ", inventory.Keys));
        return 0;
    }

    void RefreshUI()
    {
        int medicineCount = GetItemCount("Medicine");
        int ammoCount = GetItemCount("Ammo");
        int knifeCount = GetItemCount("Knife");

        if (MedicineCountText != null)
        {
            MedicineCountText.text = "Medicine : " + medicineCount;
            Debug.Log("MedicineCountText 업데이트: Medicine : " + medicineCount);
        }
        else
            Debug.LogError("MedicineCountText가 null입니다!");

        if (AmmoCountText != null)
        {
            AmmoCountText.text = "Ammo : " + ammoCount;
            Debug.Log("AmmoCountText 업데이트: Ammo : " + ammoCount);
        }
        else
            Debug.LogError("AmmoCountText가 null입니다!");

        if (KnifeCountText != null)
        {
            KnifeCountText.text = "Knife : " + knifeCount;
            Debug.Log("KnifeCountText 업데이트: Knife : " + knifeCount);
        }
        else
            Debug.LogError("KnifeCountText가 null입니다!");
    }

    void UseItem(string itemName)
    {
        if (!inventory.ContainsKey(itemName))
        {
            MessageText.text = itemName + "을(를) 찾을 수 없습니다";
            Debug.LogError(itemName + "이(가) 인벤토리에 없습니다!");
            return;
        }

        if (inventory[itemName] <= 0)
        {
            MessageText.text = itemName + "의 개수가 부족합니다";
            return;
        }

        inventory[itemName]--;
        RefreshUI();
        SaveInventory(itemName);
    }

    public void OnClickUseMedicine()
    {
        UseItem("Medicine");
    }

    public void OnClickUseAmmo()
    {
        UseItem("Ammo");
    }

    public void OnClickUseKnife()
    {
        UseItem("Knife");
    }

    void SaveInventory(string itemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).Child("Inventory").SetRawJsonValueAsync(inventoryJson).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "데이터 저장 실패";
                    Debug.LogError("저장 실패: " + task.Exception);
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                MessageText.text = itemName + " 사용 완료";
                Debug.Log(itemName + " 저장 완료");
            });
        });
    }

    void Update()
    {

    }
}
