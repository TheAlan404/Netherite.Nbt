# MineSharp.Nbt

This is the "fork" of fNbt (90% rewritten so idk if its called a fork anymore)

[Named Binary Tag (NBT)](https://minecraft.gamepedia.com/NBT_format) library for modern C#

## Features:
- Implicit casts and initializer syntax allow for better code (example below)
- Non-complicated straightforward API:
   - `NbtDocument.FromBinary(byte[])`
   - `NbtDocument.FromBinary(Stream)`
   - `byte[] NbtDocument.ToBinary()`
- Newtonsoft.JSON-like `NbtConvert`
- gzip + zlib support

## TODO
- endian support
- more docs

## EXAMPLES
###  Implicit casts and initializer syntax
```cs
	
```

#### Accessing tags (long/strongly-typed style)
```cs
    int intVal = myCompoundTag.Get<NbtInt>("intTagsName");
    string listItem = myStringList.Get<NbtString>(0);
    byte nestedVal = myCompTag.Get<NbtCompound>("nestedTag")
                              .Get<NbtByte>("someByteTag");
```

#### Accessing tags (shortcut style)
```cs
    int intVal = myCompoundTag["intTagsName"];
    string listItem = myStringList[0];
    byte nestedVal = myCompTag["nestedTag"]["someByteTag"];
```

#### Iterating over all tags in a compound/list
```cs
    foreach(NbtTag tag in myCompoundTag.Tags)
	{
        Console.WriteLine(tag.Name + " = " + tag.TagType);
    }
    foreach(string tagName in myCompoundTag.Tags.Keys)
	{
        Console.WriteLine(tagName);
    }
    for(int i = 0; i < myListTag.Count; i++)
	{
        Console.WriteLine(myListTag[i]);
    }
    foreach(NbtInt intItem in myIntList.ToArray<NbtInt>())
	{
        Console.WriteLine(intItem);
    }
```

#### Constructing a new document
```cs
    var serverInfo = new NbtCompound("Server");
    serverInfo.Add( new NbtString("Name", "BestServerEver") );
    serverInfo.Add( new NbtInt("Players", 15) );
    serverInfo.Add( new NbtInt("MaxPlayers", 20) );
```

#### Constructing using collection initializer notation
```cs
    var nbt = new NbtDocument {
        { "someInt", 123 },
        { "byteList", {
            new NbtByte(1),
            new NbtByte(2),
            new NbtByte(3)
        } },
        {
			"nestedCompound",
			{
				{ "pi", (double)3.14 }
       		}
		}
    };
```

#### Pretty-printing
```cs
    Console.WriteLine(myNbt.ToString());
```
