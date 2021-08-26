using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayFabManager
{
    public void Login(string playerName, Action<LoginResult> onsuccess)
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = GetUniqueID() + "-" + playerName,
            CreateAccount = true,             
        };

        PlayFabClientAPI.LoginWithCustomID(request, onsuccess, OnError);
    }        

    void OnError(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

    public void RecordScore(string playerName, int score, Action<UpdatePlayerStatisticsResult> onsuccess)
    {
        SetName(playerName);

        Login(playerName, (result) =>
        {            
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                         StatisticName = "HighScores",
                         Value = score
                    }
                }
            };

            var accinfoRequest = new GetAccountInfoRequest
            {
                PlayFabId = result.PlayFabId
            };

            PlayFabClientAPI.GetAccountInfo(accinfoRequest, (result) =>
            {
                if (string.IsNullOrEmpty(result.AccountInfo.TitleInfo.DisplayName))
                {
                    var updateRequest = new UpdateUserTitleDisplayNameRequest
                    {
                        DisplayName = playerName.PadRight(4,Convert.ToChar("_"))
                    };
                    PlayFabClientAPI.UpdateUserTitleDisplayName(updateRequest, (result) =>
                    {
                        PlayFabClientAPI.UpdatePlayerStatistics(request, onsuccess, OnError);
                    }, OnError);
                }
                else
                {
                    PlayFabClientAPI.UpdatePlayerStatistics(request, onsuccess, OnError);
                }                
            }, OnError);            
        });
    }

    public void GetHighScores(Action<GetLeaderboardResult> renderScores)
    {        
        var request = new GetLeaderboardRequest
        {
            StatisticName = "HighScores",
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, renderScores, OnError);
    }

    public string GetUniqueID()
    {
        string deviceId;

        if (!PlayerPrefs.HasKey("UniqueIdentifier"))
        {
            PlayerPrefs.SetString("UniqueIdentifier", Guid.NewGuid().ToString());
            PlayerPrefs.Save();
        }
            
        deviceId = PlayerPrefs.GetString("UniqueIdentifier");

        return deviceId;
    }

    public string GetName()
    {
        string playerName;        

        playerName = PlayerPrefs.GetString("playerName");

        return playerName;
    }

    public void SetName(string playerName)
    {        
        PlayerPrefs.SetString("playerName", playerName);
        PlayerPrefs.Save();        
    }
}
