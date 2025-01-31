﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun; 
using Photon.Realtime;
using TMPro;
using UnityEngine.LowLevel;
//using UnityStandardAssets.Characters.ThirdPerson;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Creates and updates the gravies on platform
/// handles player gravy detection and awarding 
/// </summary>
public class GravyManager : MonoBehaviourPunCallbacks
{
    
    #region Gameplay Values
    
    /// <summary>
    /// The percent of platforms that have gravies. 
    /// </summary>
    public float gravyPercent; 
    
    #endregion
    
    #region  Implementation Values
    
    public GameObject gravyPrefab;

    /// <summary>
    /// starting number of gravies in the game 
    /// </summary>
    [HideInInspector] public int StartingGravyNum => (int)(gravyPercent * platformManager.PlatformNum);

    
    /// <summary>
    /// number of gravy's left in the game. synced across all clients
    /// </summary>
    [HideInInspector]
    public int CurrentGravyNum
    {
        get => _currentGravyNum;
        set
        {
            _currentGravyNum = value;
            OnGravyNumChanged.Invoke(value);
        }
    }
    private int _currentGravyNum;
    public IntEvent OnGravyNumChanged = new IntEvent(); 

    
    private PlatformManager platformManager;
    private PlayerMovement playerTPC;
    
    /// <summary>
    /// index refers to children platforms. true if theres a gravy there
    /// DO NOT edit directly, use SetCustomValues["gravy_array"] instead
    /// </summary>
    private bool[] SYNC_gravyArray;
    
    
    #endregion

    #region Unity Callbacks 
    
    
    
    /// <summary>
    /// Spawns the gravies on the platforms by loading the GravyArray from PhotonCustomProperties or generating them if the client is the first master.
    /// </summary>
    public void LoadGravies()
    {        
        
        if (!PhotonNetwork.IsConnected)
            return; 
        
        platformManager = GetComponent<PlatformManager>();

        //set up listeners for landing gravy detection 
        PlayerIdentity.CallOnLocalPlayerSet(AddPlayerListeners);
        
        //generate or load gravies 
        //todo instead of checking if playercount is 1, check if its the start of a new round
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 1) // you're the first one in the game and gotta set it up 
        {
            GenerateGravyArray(platformManager.PlatformNum,StartingGravyNum);
        }
        else
        {
            UpdateGravyObjects();
        }
    }

    public static int GetGravylessPlatform()
    {
        bool[] respawnPlatforms = (bool[]) PhotonNetwork.CurrentRoom.CustomProperties["gravy_array"];
        int j = UnityEngine.Random.Range(0, respawnPlatforms.Length);
        while (respawnPlatforms[j] == true)
        {
            j = UnityEngine.Random.Range(0, respawnPlatforms.Length);
        }

        return j; 
    }
    
    #endregion
    
    #region PUN Callbacks 
    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        //todo optimize maybe 
        if (propertiesThatChanged.ContainsKey("gravy_array"))
        {
            UpdateGravyObjects();


            //regen gravies if empty 
            if (PhotonNetwork.IsMasterClient)
            {
                if (CurrentGravyNum == 0)
                {
                    GenerateGravyArray();
                }
            }
        }
    }
    #endregion

    #region  RPC

    /// <summary>
    /// Master Client awards gravy to players after checking to see if gravy is there
    /// </summary>
    /// <param name="platNum"></param>
    /// <param name="info"></param>
    [PunRPC]
    void RPC_ProcessGravyGet(int platNum, PhotonMessageInfo info)
    {
        if (SYNC_gravyArray[platNum]) // gravy is there
        {
            //award the player
            GameObject senderGO = (GameObject)info.Sender.TagObject;
//            senderGO.GetComponent<PlayerIdentity>().Gravies++; 
            
            //delete the gravy 
            removeGravy(platNum);
        }
    }
    

    #endregion

    #region Public Functions

    /// <summary>
    /// return the 
    /// 
    /// of any platform with gravy. 
    /// </summary>
    /// <returns></returns>
    public Vector3 GetGraviedPlatformPosition()
    {
        for (int i = 0; i < SYNC_gravyArray.Length; i++)
        {
            if (SYNC_gravyArray[i])
            {
                return platformManager.platformParent.transform.GetChild(i).transform.position; 
            }
        }
        return Vector3.zero; 
    }
    #endregion
    
    #region Private Functions

    /// <summary>
    /// to be called after the player is set
    /// </summary>
    void AddPlayerListeners(GameObject localPlayer)
    {
        playerTPC = localPlayer.GetComponent<PlayerMovement>();
        playerTPC.OnPlatformBelowChange.AddListener(CheckPlayerGetGravy);
    }


    /// <summary>
    /// updates gravy variables AND spawns or deletes grav display based on Gravy Array 
    /// updates CurrentGravyNum 
    /// </summary>
    void UpdateGravyObjects()
    {
        SYNC_gravyArray = (bool[])PhotonNetwork.CurrentRoom.CustomProperties["gravy_array"]; 
        CurrentGravyNum = SYNC_gravyArray.Count(s => s == true);
        
        //todo remove reduncies where this is run multiple times needlessly
        for (int i = 0; i < SYNC_gravyArray.Length; i++)
        {
            GameObject platform = platformManager.platformParent.transform.GetChild(i).gameObject;
            if (SYNC_gravyArray[i] && !HasGravyDisplay(i))
            {
                GameObject gravy = Instantiate(gravyPrefab, platform.transform);
                gravy.transform.localPosition = Vector3.zero + Vector3.up * 0.33f;
            }

            if (!SYNC_gravyArray[i] && HasGravyDisplay(i))
            {
                Destroy(platform.transform.GetChild(1).gameObject); //todo get gravy with set or send signal, to only delete gravy not every child  
            }
        }
    }
    
    /// <summary>
    /// generates a gravy array, a bool table referring to platforms with gravies on them, and sets it to
    /// room custom properties
    /// </summary>
    /// <param name="pNum"></param>
    /// <param name="gNum"></param>
    void GenerateGravyArray(int pNum, int gNum) 
    { 
        bool[] gravyArray = Utility.GetRandomBoolArray(pNum, gNum);
        
        Hashtable h = new Hashtable();
        h.Add("gravy_array", gravyArray);

        PhotonNetwork.CurrentRoom.SetCustomProperties(h);
    }
    
    /// <summary>
    /// reset the game with current gravy count settings
    /// </summary>
    public void GenerateGravyArray() 
    {
        GenerateGravyArray(platformManager.PlatformNum,
            (int)(gravyPercent * platformManager.PlatformNum));
    }

    /// <summary>
    /// returns true if gravy is already displayed on that platform
    /// todo make it detect actual gravy objects insteasd of count childs
    /// </summary>
    /// <param name="childIndex"></param>
    /// <returns></returns>
    bool HasGravyDisplay(int childIndex)
    {
        return platformManager.platformParent.transform.GetChild(childIndex).childCount > 1; 
    }

    
    
    /// <summary>
    /// checks if player is getting a platform by LANDING on it, if yes send it to master client for processsing
    /// </summary>
    void CheckPlayerGetGravy(GameObject platform)
    {
        if (platform == null)
            return; 
        
        if (SYNC_gravyArray == null || SYNC_gravyArray.Length == 0)
            return; 
        
        int actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
        int platNum = platform.transform.GetSiblingIndex();
        bool touchedGravy = SYNC_gravyArray[platNum];

        if (touchedGravy)
        {
            photonView.RPC("RPC_ProcessGravyGet", RpcTarget.MasterClient, platNum);
        }
    }
    /// <summary>
    /// removes the gravy by updating the GravyArray in setcustomproperties 
    /// </summary>
    /// <param name="platIndex"></param>
    void removeGravy(int platIndex)
    {
        SYNC_gravyArray[platIndex] = false; 
        Hashtable h = new Hashtable {{"gravy_array", SYNC_gravyArray}};
        PhotonNetwork.CurrentRoom.SetCustomProperties(h); 
    }
    #endregion
    
}