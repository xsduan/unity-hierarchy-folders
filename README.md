# Folders for Unity Hierarchy

Specialized folder objects for Unity Hierarchy.

## Installation

This uses the new UPM system introduced in `2017.2` (and something something
`2018.1`?). The old copy-into-Assets method still works perfectly decent so if
you don't want to bother with UPM just copy the `Editor` and `Runtime` folders
into your project.

In any case, simply add a relative link to this folder in your `manifest.json`
and hopefully this will show up, e.g.:

```json
{
  "dependencies": {
    "com.unity.package-manager-ui": "1.9.11",
    "com.xsduan.heirarchy-folders": "file:../../unity-hierarchy-folders"
  }
}
```

A "Create Folder" menu item should show up in the GameObject menu. Add
`Tests/Example.unity` to your current scene for an example of what hierarchy
folders can do for you.

The UPM does not have much documentation at the moment so it probably will be
buggy, you're not going crazy!

## Possible FAQs

### Why folders in the first place?

As projects get bigger, they tend to get cluttered in the scene. It's very
helpful if you can group them together into logical groups.

#### Why delete them on build then?

Because they are best used for level designers to declutter the hierarchy, but
calculating the global transform from the local during runtime can take a
noticeable impact on performance once scenes get to 1000, 10000, or more
objects.

#### So why can't I just use empty GameObjects and delete them on build?

Sometimes empty GameObjects are used for other things and it's useful to have a
specific type of object that should always be deleted on build.

Besides, I did all the legwork, so you wouldn't have to!

### There's another product/widget that exists that does this exact task.

So there are. This isn't exactly a unique concept and I only made it for future
personal use and shared it only to possibly to help other people because I
couldn't find it on Google.

In fact, there's probably like 50 versions of this floating around that I can't
google-fu my way through. I personally have seen one on the Asset Store myself,
but it was $0.99/seat and as you can tell it took me maybe 3 hours to set up
their entire product plus auto pruning so it's clearly not worth paying jack
for.

If you are the owner of one such product, please contact me and we can work
something out.
