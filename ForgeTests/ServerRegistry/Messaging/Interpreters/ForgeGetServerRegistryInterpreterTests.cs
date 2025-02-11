﻿using System.Linq;
using System.Net;
using FakeItEasy;
using Forge.Networking;
using Forge.Networking.Messaging;
using Forge.Networking.Sockets;
using Forge.ServerRegistry.Messaging.Messages;
using ForgeServerRegistryService.Messaging.Interpreters;
using ForgeServerRegistryService.Networking.Players;
using NUnit.Framework;

namespace ForgeTests.ServerRegistry.Messaging.Interpreters
{
	[TestFixture]
	public class ForgeGetServerRegistryInterpreterTests : ForgeNetworkingTest
	{
		/// <summary>
		/// The SendReliableMessage TTL parameter must match the TTL paremeter
		/// in GetServerRegistryInterpreter.cs method Interpret.  If this
		/// method changes, update this test accordingly
		/// </summary>
		[Test]
		public void InterpretationOfGetRegistryMessage_ShouldGetCorrectPlayerList()
		{
			var players = new RegisteredServer[]
			{
				new RegisteredServer { EndPoint = new IPEndPoint(IPAddress.Parse(CommonSocketBase.LOCAL_IPV4), 15937), IsRegisteredServer = true },
				new RegisteredServer { EndPoint = new IPEndPoint(IPAddress.Parse(CommonSocketBase.LOCAL_ANY_IPV4), 15938), IsRegisteredServer = true }
			};
			var net = A.Fake<INetworkMediator>();
			A.CallTo(() => net.PlayerRepository.Count).Returns(players.Length);
			A.CallTo(() => net.PlayerRepository.GetEnumerator()).Returns(players.ToList().GetEnumerator());
			A.CallTo(() => net.MessageBus.SendReliableMessage(A<IMessage>._, A<ISocket>._, A<EndPoint>._, 0))
				.Invokes((ctx) =>
				{
					Assert.IsTrue(ctx.Arguments[0] is ForgeServerRegistryMessage);
					var msg = (ForgeServerRegistryMessage)ctx.Arguments[0];
					Assert.AreEqual(players.Length, msg.Entries.Length);
					for (int i = 0; i < players.Length; i++)
					{
						var ep = new IPEndPoint(IPAddress.Parse(msg.Entries[i].Address), msg.Entries[i].Port);
						Assert.AreEqual(players[i].EndPoint, ep);
					}
				});
			var interpreter = new GetServerRegistryInterpreter();
			interpreter.Interpret(net, players[0].EndPoint, A.Fake<IMessage>());
			A.CallTo(() => net.MessageBus.SendReliableMessage(A<IMessage>._, A<ISocket>._, A<EndPoint>._, 0))
				.MustHaveHappenedOnceExactly();
		}

		[Test]
		public void InterpretationOfGetRegistryMessageWithMixedPlayers_ShouldGetCorrectServerList()
		{
			var players = new RegisteredServer[]
			{
				new RegisteredServer { EndPoint = new IPEndPoint(IPAddress.Parse(CommonSocketBase.LOCAL_IPV4), 15937), IsRegisteredServer = true },
				new RegisteredServer { EndPoint = new IPEndPoint(IPAddress.Parse(CommonSocketBase.LOCAL_ANY_IPV4), 15938), IsRegisteredServer = true },
				new RegisteredServer { EndPoint = new IPEndPoint(IPAddress.Parse("5.5.5.5"), 15990), IsRegisteredServer = false },
				new RegisteredServer { EndPoint = new IPEndPoint(IPAddress.Parse("128.33.21.0"), 15933), IsRegisteredServer = true },
			};
			var net = A.Fake<INetworkMediator>();
			A.CallTo(() => net.PlayerRepository.Count).Returns(players.Length);
			A.CallTo(() => net.PlayerRepository.GetEnumerator()).Returns(players.ToList().GetEnumerator());
			A.CallTo(() => net.MessageBus.SendReliableMessage(A<IMessage>._, A<ISocket>._, A<EndPoint>._, 0))
				.Invokes((ctx) =>
				{
					Assert.IsTrue(ctx.Arguments[0] is ForgeServerRegistryMessage);
					var msg = (ForgeServerRegistryMessage)ctx.Arguments[0];
					Assert.AreEqual(players.Count(p => p.IsRegisteredServer), msg.Entries.Length);
					for (int i = 0, idx = 0; i < players.Length; i++)
					{
						if (!players[i].IsRegisteredServer)
							continue;
						var ep = new IPEndPoint(IPAddress.Parse(msg.Entries[idx].Address), msg.Entries[idx].Port);
						Assert.AreEqual(players[i].EndPoint, ep);
						idx++;
					}
				});
			var interpreter = new GetServerRegistryInterpreter();
			interpreter.Interpret(net, players[0].EndPoint, A.Fake<IMessage>());
			A.CallTo(() => net.MessageBus.SendReliableMessage(A<IMessage>._, A<ISocket>._, A<EndPoint>._, 0))
				.MustHaveHappenedOnceExactly();
		}
	}
}
