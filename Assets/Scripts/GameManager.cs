﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
//using UnityStandardAssets.Characters.ThirdPerson;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// handles scoreboard to display and increments kills on killers 
/// Hides room when it is half capacity so that only code joiners can enter
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    #region Gameplay Values
    #endregion

    #region Implementation Values

    //Component References
    public static GameManager instance;
   
    public PlatformManager platformManager; //quick ref 

    //UI (todo move out?) 
    public TextMeshProUGUI killFeed;
    public TextMeshProUGUI leaderBoardDisplay;


    public List<Player> leaderBoard;
    public Player[] playerList;


    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        //init
        instance = this;
        platformManager = GetComponent<PlatformManager>(); 
    }

    // Start is called before the first frame update
    void Start()
    {
        SetSpawn(); 
    }

    private void Update()
    {
        //application stuff
        if (Input.GetButtonDown("Cancel"))
        {
            PhotonNetwork.LeaveRoom();
        }
    }


    #endregion

    #region RPCs

    /// <summary>
    /// Announces that the local player was killed 
    /// </summary>
    /// <param name="killerActorNumber"></param>
    /// <param name="info"></param>
    public void ReportFall(int killedNumber, int killerNumber)
    {
        PlayerIdentity deadPlayer = PlayerIdentity.GetPlayer(killedNumber); 
        PlayerIdentity killer = PlayerIdentity.GetPlayer(killerNumber);

        if (killer == null) // if they fell without being attacked
        {
            SetKillFeed($"{deadPlayer.NickName} is gone.");
        }
        else
        {
            //todo support bot kill incrementation later 
            //increment kill on killer 
            if (PhotonNetwork.LocalPlayer.ActorNumber == killerNumber) //if local player is killer 
            {
                PlayerIdentity.localPlayerInstance.Kills++; 
            }

            //tell people who died via killfeed
            SetKillFeed($"{killer.NickName} ended {deadPlayer.NickName}.");
        }  
    }


    #endregion
    
    #region Pun Callbacks

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if(changedProps.ContainsKey("kills"))
            UpdateLeaderboard();
    }

    //room roster changes
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerList = PhotonNetwork.PlayerList; //refresh player name list

        if (PhotonNetwork.IsMasterClient)
            HideRoomIfHalfFull(); 

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        playerList = PhotonNetwork.PlayerList;

        if (PhotonNetwork.IsMasterClient)
            HideRoomIfHalfFull();

    }



    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }
    
    #endregion

    #region  Private Methods

    /// <summary>
    /// Sorts players by kills and outputs the text to the leaderBoardDisplay.
    /// </summary>
    void UpdateLeaderboard()
    {
        leaderBoard = playerList.ToList();
        leaderBoard.Sort(ComparePlayerKills);

        string lbString = "";
        foreach (Player p in leaderBoard)
        {
            lbString += $"{p.NickName} {p.CustomProperties["kills"]}\n";
        }

        leaderBoardDisplay.text = lbString;
    }

    int ComparePlayerKills(Player p1, Player p2)
    {
        ///tood null reference here sometimes 
        if (!p1.CustomProperties.ContainsKey("kills") || !p2.CustomProperties.ContainsKey("kills"))
        {
            print("ERROR: kills not intialized");
            return 0; 
        }

        return (int) p2.CustomProperties["kills"] - (int) p1.CustomProperties["kills"];
    }

    void SetKillFeed(string s)
    {
        killFeed.text = s;
    }

    
    void SetSpawn()
    {

        //init local player properties 
        Hashtable playerProps = new Hashtable { { "plat_state", 0 }, { "kills", 0 } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);


        PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);

        //todo make this suitable for bots update 
        playerList = PhotonNetwork.PlayerList;
    }

    void HideRoomIfHalfFull()
    {
        if (playerList.Length > (PhotonNetwork.CurrentRoom.MaxPlayers/2))
        {
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
    }
    #endregion
   
}
