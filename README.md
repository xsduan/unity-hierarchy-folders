# Folders for Unity Hierarchy

Creates specialized folder objects for Unity Hierarchy.

## FAQs I anticipate
### Why folders in the first place?

As projects get bigger, they tend to get cluttered in the scene. It's very helpful if you can group them together into logical groups.

#### Why delete them on build then?

Because they are best used for level designers to declutter the hierarchy, but calculating the global transform from the local during runtime can take a noticeable impact on performance once scenes get to 1000, 10000, or more objects.

#### So why can't I just use empty GameObjects and delete them on build?

Sometimes empty GameObjects are used for other things and it's useful to have a specific type of object that should always be deleted on build. Also, I did all the heavy lifting so really.

### There's another product/another repository/another widget that exists that does this exact task.

So there are. This isn't exactly a unique concept and I only made it for future personal use and shared it only to possibly to help other people because I couldn't find it on Google.

I personally have seen one on the Asset Store myself, but it was $0.99/seat and as you can tell it took me maybe 3 hours to set up their entire product plus auto pruning so it's clearly not worth paying jack for.

Also it was like 3 versions old sooooooo

#### You've stole my product/my project code!

##### For paid products

I can assure you that I have made this completely from scratch. If feel you can show that more than 90% of this repository functionally matches your code, I will work something out.

##### For projects

If it is open source, I will gladly contribute to yours, just point me in the right direction.

## How to install

Drag the folder into your project and it should add all the necessary components automatically. Create Folder should be in the right-click menu on the hierarchy after the editor reloads.
