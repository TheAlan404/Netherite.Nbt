Current released version is 0.3.0 (28 September 2012).

LibNbt2012 is an effort to rewrite Erik Davidson's (aphistic's) LibNbt library, for
improved performance, ease of use, and reliability. Notable changes made so far include:

- Auto-detection of NBT file compression.
- Loading and saving ZLib (RFC-1950) compresessed NBT files.
- Reduced loading/saving CPU use by 15%, and memory use by 40%
- NbtCompound now implements ICollection<NbtTag>, and NbtList implements IList<NbtTag>
- Added full support for TAG_Int_Array
- Added more constraint checks to tag loading, modification, and saving.
- Replaced getter/setter methods with properties, wherever possible.
- Expanded unit test coverage.
- Fully documented everything.

Query API has been removed from the library.


README for LibNbt v0.2.0 preserved below:
=============
Website: http://www.github.com/aphistic/libnbt/

LibNbt is a library to read and write the Named Binary Tag (NBT) file format created by
Markus Persson (a.k.a. Notch, http://notch.tumblr.com/) for saving Minecraft level data.

The documentation is currently lacking but at the bottom of this document are a few examples
to get you up and running.

If you do use this library I would really appreciate it if you acknowledge the work I've
put into it by giving me some kind of credit in your documentation or application but it's
not required.  Even better would be to let me know the projects you're using it in!

If you run into any issues or features you'd like to see PLEASE let me know!  You can access
the issue tracker on the project homepage at: http://github.com/aphistic/libnbt/issues
