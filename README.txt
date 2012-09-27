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

Upcoming
========
- Implement an NAnt build process for easier multiplatform builds.
- Write documentation.
- Continue writing unit tests.

Querying
========
Querying is implemented with a slash separated format.  Each part of the query is either
a tag name or an index (in the case of NbtList). All queries must start with a slash.

The query examples below use the bigtest.nbt test file tag hierarchy.  You can find the
layout of this file in LibNbtTest/TestFiles/bigtest.nbt.txt.

To get the "shortTest" tag you would use the query:
/Level/shortTest

To get the "ham" tag of the "nested compound test" tag, you would use the query:
/Level/nested compound test/ham

You can also use indexes to reference specific tags in NbtList tags. This query would
return the NbtLong with the value 13 in bigtest. Note that indexes start at 0:
/Level/listTest (long)/2

Lastly, you can combine named tags and list tags into the same query. The following query
would return the "name" NbtString of the 2nd compound in the "listTest (compound)" tag:
/Level/listTest (compound)/1/name

Queries are executed by calling the Query(string query) or Query<T>(string query) functions
on a tag object.  The generic function Query<T>(string query) allows you to specify the type
of the return object.  The non-generic version Query(string query) will return all results as
an NbtTag.
