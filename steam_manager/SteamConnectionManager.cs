using Steamworks;
using Steamworks.Data;
using System;
using Godot;

// 'client' logic
public class SteamConnectionManager : ConnectionManager{
	public override void OnConnected(ConnectionInfo info)
	{
		base.OnConnected(info);
		GD.Print("SCM: on connection");
	}

	public override void OnConnecting(ConnectionInfo info)
	{
		base.OnConnecting(info);
		GD.Print("SCM: on connecting");
	}

	public override void OnDisconnected(ConnectionInfo info)
	{
		base.OnDisconnected(info);
		GD.Print("SCM: on disconnection " + info.EndReason);
	}

	public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		base.OnMessage(data, size, messageNum, recvTime, channel);
		SteamMessageUtils.ProcessMessage(data, size);
	}
}
