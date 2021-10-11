using UnityEngine;

//シングルトン化する継承先クラスの宣言は以下のようにする
//public class MyManager : Singleton<MyManager>{}

//シングルトン化したクラス絵のアクセス方法
//MyManager.Instance.Func();

public class Singleton<T> : MonoBehaviour
     where T: MonoBehaviour
{
    private static T _instance = null;

    public static T Instance
    {
        get
        {
            if(_instance==null)
            {
                //インスペクターにあるかチェック、ある場合は取得して終了
                _instance = FindObjectOfType<T>();
                if(_instance==null)
                {
                    //インスペクターに無ければ制作する
                    _instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
                    Debug.LogWarning("指定したシングルトンのオブジェクトが見つからなかったので制作＝" + typeof(T));
                }
            }
            return _instance;
        }
    }

}
