using Godot;
using Steamworks;
using Steamworks.Data;
using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Collections.Generic;
using MessageCallbackWithPayload = System.Action<uint, System.Collections.Generic.Dictionary<string, string>>;


public enum MessageId : System.UInt16 {
	RESERVED
}

public enum SimplePlayerAction : System.UInt16 {
	RESERVED
}

public class SteamMessageUtils {

	public static event MessageCallbackWithPayload OnLobbyPlayerReadyMessage;
	public static event MessageCallbackWithPayload OnGameStartMessage;
	public static event MessageCallbackWithPayload OnPlayerLocationPub;
	public static event MessageCallbackWithPayload OnPlayerAttackedPub;
	public static event MessageCallbackWithPayload OnEnemiesLocationPub;
	public static event MessageCallbackWithPayload OnEnemyKilledMessage;
	public static event MessageCallbackWithPayload OnFloorPub;
	public static event MessageCallbackWithPayload OnPlayerActionMessage;
	public static event MessageCallbackWithPayload OnDoorStatePub;

	public static void ProcessMessage(IntPtr data, int size){
		Dictionary<string, string> Message = DeserializeMessage(data, size);
		if (Message == null){
			return;
		}

		MessageId IncomingMessageId = (MessageId)Convert.ToInt16(Message["message_id"]);
		uint SenderId = uint.Parse(Message["sender_id"]);
		Dictionary<string, string> Payload = null;
		if (Message.ContainsKey("payload")){
			Payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(Message["payload"]);
		}

		// DEBUG MESSAGES 
		// GD.Print(IncomingMessageId + " from " + SenderId.ToString());

		switch(IncomingMessageId){
			default:
				GD.Print("Unknown message received! " + IncomingMessageId);
				return;
		}
	}

	public static string CreateSerializedMessage(uint senderAccountId, MessageId messageId, Dictionary<string, string> payload = null){

		Dictionary<string, string> MessageDict = new Dictionary<string, string>(){
			{"message_id", Convert.ToString((short)messageId)},
			{"sender_id", senderAccountId.ToString()}
		};

		if (payload != null){
			MessageDict.Add("payload", JsonConvert.SerializeObject(payload));
		}

		// TODO add check for greater than max message size 1024 * 512 bytes

		return JsonConvert.SerializeObject(MessageDict);
	}

	private static Dictionary<string, string> DeserializeMessage(IntPtr data, int size){

		byte[] managedArray = new byte[size];
		Marshal.Copy(data, managedArray, 0, size);
		var str = System.Text.Encoding.Default.GetString(managedArray);
		try{
			Dictionary<string, string> message = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
			return message;
		}catch(Exception e){
			GD.Print("Failed to deserialize game message: " + e.Message + " message contents: " + str);
		}

		return null;
	}
}
