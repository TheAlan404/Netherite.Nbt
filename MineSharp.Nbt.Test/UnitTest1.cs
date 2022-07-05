using DeepSlate.Nbt.Entities;

#pragma warning disable CS8602

namespace DeepSlate.Nbt.Test
{
	[TestClass]
	public class Ctors
	{
		[TestMethod]
		public void NbtDocumentTest1()
		{
			var nbt = new NbtDocument()
			{
				{ "servers", new NbtList(){
					new NbtCompound()
					{
						{ "ip", "localhost" },
						{ "name", "my local server" },
						{ "acceptTextures", true },
					}
				} }
			};

			Console.WriteLine(nbt.ToString());
		}

		[TestMethod]
		public void NbtDocumentTest2()
		{
			var nbt = new NbtDocument();
			nbt["servers"] = new NbtList()
			{
				new NbtCompound()
				{
					{ "ip", "localhost" },
					{ "name", "my local server" },
				}
			};

			Console.WriteLine(nbt.ToString());
		}

		[TestMethod]
		public void NbtDocumentTest3()
		{
			var nbt = new NbtDocument();

			var list = new List<NbtTag>();
			
			list.Add(new Dictionary<string, NbtTag>()
			{
				{ "ip", "localhost" },
				{ "name", "my local server" },
			});

			nbt["servers"] = list;

			Console.WriteLine(nbt.ToString());
		}

		[TestMethod]
		public void NbtDocumentTest4()
		{
			var nbt = new NbtDocument();

			var list = new List<NbtCompound>()
			{
				new()
				{
					{ "ip", "localhost" },
					{ "name", "my local server" },
				},
			};

			nbt["servers"] = list;

			Console.WriteLine(nbt.ToString());
		}
	}

	public class NestedTester
	{
		public ServerEntry? ServerOne;
		public ServerEntry? ServerTwo;
		public string? Funny;

		public override string ToString()
		{
			return $"NestedTester\n{{\n\tServerOne = {ServerOne?.ToString().PadLeft(1, '\t')}\n\tServerTwo = {ServerTwo?.ToString().PadLeft(1, '\t')}\n\tFunny = {Funny}\n}}";
		}
	}

	public class ServerListing
	{
		public List<ServerEntry>? Servers;

		public override string ToString()
		{
			return $"Server List\n{{\n{string.Join("\n", Servers?.Select(x => x.ToString().PadLeft(2)) ?? Array.Empty<string>())}\n}}";
		}
	}

	public class ServerEntry
	{
		[NbtProperty("name")]
		public string? Name;

		[NbtProperty("ip")]
		public string? IP;

		[NbtProperty("acceptTextures")]
		public bool? AcceptTextures;

		[NbtProperty("icon")]
		public string? Icon;

		public override string ToString()
		{
			return $"ServerEntry\n{{\n\tName = {Name}\n\tIP = {IP}\n\tAcceptTextures = {AcceptTextures}\n\tIcon = {Icon}\n}}";
		}
	}

	[TestClass]
	public class ConverterTests
	{
		[TestMethod]
		public void NbtDocumentToServerEntry()
		{
			var nbt = new NbtDocument()
			{
				{ "ip", "localhost" },
				{ "name", "my local server" },
			};

			var obj = NbtConvert.DeserializeObject<ServerEntry>(nbt);
			Console.WriteLine(obj.ToString());
		}

		[TestMethod]
		public void NestedTest()
		{
			var nbt = new NbtDocument()
			{
				{ "ServerOne", new NbtCompound(){
					{ "ip", "hypixel.net" },
					{ "name", "rip techno" },
				} },
				{ "ServerTwo", new NbtCompound(){
					{ "ip", "localhost" },
					{ "name", "local server h" },
				} },
				{ "Funny", "this looks like a job for me" },
			};

			var obj = NbtConvert.DeserializeObject<NestedTester>(nbt);
			Console.WriteLine(obj.ToString());
		}

		[TestMethod]
		public void ListTest()
		{
			var nbt = new NbtDocument()
			{
				{ "Servers", new NbtList(){
					new NbtCompound()
					{
						{ "ip", "hypixel.net" },
						{ "name", "rip techno" },
					},
					new NbtCompound()
					{
						{ "ip", "localhost" },
						{ "name", "local server h" },
					},
					new NbtCompound()
					{
						{ "ip", "example.com" },
						{ "name", "wen you run out of example text ideas" },
						{ "acceptTextures", (byte)0x01 },
					},
				} },
			};

			var obj = NbtConvert.DeserializeObject<ServerListing>(nbt);
			Console.WriteLine(obj.ToString());
		}

		[TestMethod]
		public void Real()
		{
			byte[] s = File.ReadAllBytes("./servers.dat");

			NbtDocument nbt = NbtDocument.FromBinary(s);

			Console.WriteLine(nbt.ToString());
		}
	}
}