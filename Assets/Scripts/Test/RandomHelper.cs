using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace RandomTool
{
	
	public class RandomHelper
	{
        static List<Vector2Int> Range_List = new List<Vector2Int>();//區段最小值
        static int min;

       
        public static T GetRandomList<T>(List<T> list) where T: RandomObject
		{
            //傳入空 或者 無內容 則回傳null
			if (list == null  || list.Count <= 0)
			{
                Debug.Log("無輸入權重表列");
				return null;
			}
            


            //設置權重區段
            Range_List.Clear();          
            min = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    min = Range_List[i - 1].y;
                }
                Range_List.Add(new Vector2Int(min, min + list[i].Weight));
            }
            min = Range_List[Range_List.Count - 1].x;
            int totalWeight = Range_List[Range_List.Count - 1].y;
            Range_List[Range_List.Count - 1] = new Vector2Int(min, totalWeight + 1);

            Debug.Log("totalWeight:" + totalWeight);



            //Random ran = new Random(GetRandomSeed());
            int ranValue=UnityEngine.Random.Range(0, totalWeight);
            int ranIndex = -1;//區段指標 預設-1

            for (int i = 0; i < list.Count; i++)
            {
                if (ranValue >= Range_List[i].x && ranValue < Range_List[i].y)
                {
                    ranIndex = i;
                    break;
                }
            }
            return list[ranIndex];
          
        }


		/// <summary>
		/// 随机种子值
		/// </summary>
		/// <returns></returns>
		private static int GetRandomSeed()
		{
			byte[] bytes = new byte[4];
			System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
			rng.GetBytes(bytes);
			return BitConverter.ToInt32(bytes, 0);
		}

       


    }



	/// <summary>
	/// 权重对象
	/// </summary>
	[System.Serializable]
	public class RandomObject
	{
		/// <summary>
		/// 权重
		/// </summary>
		public int Index { private set; get; }
        public int Weight;// { set; get; }
        public string Name;
        public void SetIndex(int _Value)
		{
			Index = _Value;
        }
       
	}

    public class RandomDataObject<T> : RandomObject
    {
        public T data;
    }
}