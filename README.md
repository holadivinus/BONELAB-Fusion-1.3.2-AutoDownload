#Garbage Proof of Concept

This whole practice was just because:
1. I blundered and couldn't explain my idea in the discord
2. I said that i'd set up a proof of concept (crazy reason, i know)

this """"""release"""""" works with vanilla players

The full barcode of an avatar (which has the pallets barcode prefixed) 
is synced to each client, so each client could search through 
the repositories linked in their repositories.txt file for the 
associated pallet.

I understand that more data about the avatar's pallet would need to be synced 
for security and updates, this is just to show that it's possible in a crude fashion

FYI due to some SLZ code shenanigans, this mod will only search repositories
linked in the repositories.txt in your BONELABS Installation folder

FYI 2. This is dependant on JevilBL (https://bonelab.thunderstore.io/package/extraes/JeviLibBL/)
Since i need to use an await on a Unitask. In order for JevilBL to work you'll need to launch
the game with it installed twice, so it can rebuild melonloaders managed assemblies with the
missing Unitask.Awaitter's interfaces.


There are some obvious bugs that will stop certain avatars from loading, just due to how
it's parseing and verifying barcodes
