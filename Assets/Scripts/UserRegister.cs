using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class UserRegister : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] InputField NickNameInput;
    [SerializeField] Text checkText;

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shinguexam2601039-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
    }

    public void OnClickRegister()
    {
        string nickName = NickNameInput.text.Trim();

        if (string.IsNullOrEmpty(nickName))
        {
            checkText.text = "닉네임을 입력하세요.";
            return;
        }

        reference.Child("UserInfo").OrderByChild("NickName").EqualTo(nickName).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "Firebase 읽기 오류";
                });
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (snapshot.HasChildren)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "이미 사용 중인 닉네임입니다.";
                });
                return;
            }
            CreateUser(nickName);
        });
    }

    void CreateUser(string nickName)
    {
        DatabaseReference newUserRef = reference.Child("UserInfo").Push();
        string userKey = newUserRef.Key;

        Dictionary<string, int> inventory = new Dictionary<string, int>();
        inventory["Medicine"] = 0;
        inventory["Ammo"] = 0;
        inventory["Knife"] = 0;

        Dictionary<string, bool> unitList = new Dictionary<string, bool>();
        unitList["Unit1"] = true;
        for (int i = 2; i <= 5; i++)
        {
            unitList["Unit" + i] = false;
        }

        string inventoryJson = JsonConvert.SerializeObject(inventory);
        string unitListJson = JsonConvert.SerializeObject(unitList);

        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "NickName", nickName },
            { "Coin", 500 },
            { "Score", 0 },
            { "Inventory", inventoryJson },
            { "UnitList", unitListJson }
        };

        newUserRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(userData)).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "회원 가입 실패";
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                checkText.text = "회원 가입 완료";
                Debug.Log("회원 가입 성공: " + nickName);
            });
        });
    }

    void Update()
    {
        
    }
}
