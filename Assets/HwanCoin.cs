using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using JsonFx.Json;

public class HwanCoin : MonoBehaviour {

    public CoinShop[] coinShops;
    public ShopName baseShopName = ShopName.Bithumb;
    public InputField inputFieldYen;
    public InputField inputFieldFee;
    public InputField inputFieldInvesting;

    private float yenRate = 9.6f;
    private static float feeRate = 1.065f;
    private static float investingValue = 0f;

    public enum PairType
    {
        BTC,
        EHT,
        ETC,
    }

    public enum ShopName
    {
        Bithumb,
        BitFlyer,
        CoinCheck,
        Zaif,
    }

    public interface ILastPrice
    {
        double GetLastPrice(string responseBody);
    }

    [System.Serializable]
    public class CoinShop
    {
        [System.Serializable]
        public class PairId
        {
            public PairType type;
            public string id;
            public Text viewText;
            public double LastPrice { get; set; }
            public double Premium { get; set; }
        }

        public ShopName shopName;
        public string url;
        public List<PairId> pairIds;
        public float feeRate;
        public ILastPrice LastPriceInterface { get; set; }

        public IEnumerator UpdateCoinRenue(CoinShop baseShop, float yenRate)
        {
            foreach (var pairId in pairIds)
            {
                WWW www = new WWW(url + pairId.id);
                yield return www;
                string result = Encoding.GetEncoding("utf-8").GetString(www.bytes);
                //var info = $"{pairId.id} : {result}";
                //Debug.Log(info);
                double price = pairId.LastPrice = LastPriceInterface.GetLastPrice(result);
                double premium = 0;
                double renue = 0;

                if (yenRate > 0)
                    pairId.LastPrice = price = price * yenRate * HwanCoin.feeRate;

                if (baseShop != null)
                {
                    var basePair = baseShop.pairIds.Find(item => item.type == pairId.type);
                    premium = pairId.Premium = (basePair.LastPrice / price) - 1.0;
                    renue = basePair.LastPrice - price;
                }

                if (investingValue > 0)
                {
                    double number = investingValue / price;
                    renue *= number;
                }

                pairId.viewText.text = ((int)price).ToString("#,##0") + " | " +
                                        (premium * 100).ToString("0.##") + "%" + " | " +
                                        ((int)renue).ToString("#,##0");
        }

            yield break;
        }
    }

    public class BithumbLastPrice : ILastPrice
    {
        public double GetLastPrice(string responseBody)
        {
            Dictionary<string, object> jsonTable = JsonReader.Deserialize<Dictionary<string, object>>(responseBody);
            return double.Parse((jsonTable["data"] as Dictionary<string, object>)["closing_price"] as string);
        }
    }

    public class BitFlyerLastPrice : ILastPrice
    {
        private double btcPrice;

        public double GetLastPrice(string responseBody)
        {
            Dictionary<string, object> jsonTable = JsonReader.Deserialize<Dictionary<string, object>>(responseBody);
            double price = (double)jsonTable["mid_price"];

            if (price < 1f)
                return price = btcPrice * (double)jsonTable["mid_price"];

            return btcPrice = price;
        }
    }

    public class CoinCheckLastPrice : ILastPrice
    {
        public double GetLastPrice(string responseBody)
        {
            Dictionary<string, object> jsonTable = JsonReader.Deserialize<Dictionary<string, object>>(responseBody);
            return double.Parse(jsonTable["rate"] as string);
        }
    }

    public class ZaifLastPrice : ILastPrice
    {
        public double GetLastPrice(string responseBody)
        {
            Dictionary<string, object> jsonTable = JsonReader.Deserialize<Dictionary<string, object>>(responseBody);
            return (double)jsonTable["last_price"];
        }
    }

    private void Start()
    {
        SetLastCoinInterface();
        Refresh();
    }

    public void OnInputYenRate(string yenRateStr)
    {
        Debug.Log(inputFieldYen.text);
        yenRate = float.Parse(inputFieldYen.text);
        Refresh();
    }

    public void OnInputFeeRate(string feeRateStr)
    {
        Debug.Log(inputFieldFee.text);
        feeRate = float.Parse(inputFieldFee.text);
        Refresh();
    }

    public void OnInputInvesting(string investingStr)
    {
        Debug.Log(inputFieldInvesting.text);
        investingValue = float.Parse(inputFieldInvesting.text);
        Refresh();
    }

    public void Refresh()
    {
        StartCoroutine(RefreshCoinInfos());
    }

    public void SetLastCoinInterface()
    {
        foreach (var coinShop in coinShops)
        {
            switch (coinShop.shopName)
            {
                case ShopName.Bithumb: coinShop.LastPriceInterface = new BithumbLastPrice(); break;
                case ShopName.BitFlyer: coinShop.LastPriceInterface = new BitFlyerLastPrice(); break;
                case ShopName.CoinCheck: coinShop.LastPriceInterface = new CoinCheckLastPrice(); break;
                case ShopName.Zaif: coinShop.LastPriceInterface = new ZaifLastPrice(); break;
            }
        }
    }

    IEnumerator RefreshCoinInfos () {

        for(int i = 0;i < coinShops.Length;++i)
        {
            if (coinShops[i].shopName == baseShopName)
            {
                var co = coinShops[i].UpdateCoinRenue(null, 0);
                yield return co;
            }
            else
            {
                var co = coinShops[i].UpdateCoinRenue(coinShops[0], yenRate);
                yield return co;
            }
        }

        /*
        string[] bitthumbPairs = new string[]
        {
            "BTC",
            "ETH",
            "ETC",
            //"XMR",
            //"XRP",
            //"LTC",
            //"DASH",
            //"BCH"
        };

        string[] bitFlyerPairs = new string[]
        {
            "", //btc
            "?product_code=ETH_BTC", //eth per btc
        };

        string[] coinCheckPairs = new string[]
        {
            "btc_jpy",
            "eth_jpy",
            "etc_jpy",
            //"xmr_jpy",
            //"xrp_jpy",
            //"ltc_jpy",
            //"dash_jpy",
            //"bch_jpy"
        };

        string[] zaifPairs = new string[]
        {
            "btc_jpy",
            "eth_jpy",
            //"bch_jpy",
        };

        string[] shopApiUrl = new string[4]
        {
            $"https://api.bithumb.com/public/ticker/",
            $"https://api.bitflyer.jp/v1/getboard",
            $"https://coincheck.com/api/rate/",
            $"https://api.zaif.jp/api/1/last_price/",
        };
        */
    }
}
