# Known Issues

This page lists all the ongoing known issues in PowerApps-Language-Tooling for which we have workarounds. These aren't necessarily bugs introduced in the pack/unpack utility but rather issues arising out of continuously evolving enhancements in the `.msapp` format that has been leading to issues when attempting to unpack apps built using older versions of Power Apps Studio. These are suggested workarounds that will help bring these apps to the latest format specification. This document will be continuously updated as issues and workarounds are discovered.

## Warning PA2001: Checksum mismatch. AppTests\2.json checksum does not match on extract

This is a known issue with apps developed with a previous version of Studio, and has now been resolved. This error is known to occur due to a stray empty test step that gets added when unpacking the msapp. This can be resolved by adding an action to the empty test step and then deleting the test entirely by the following steps -

1. Open the msapp back into the Power Apps Studio
2. Navigate to Tests
3. Edit the empty test step to add any non-empty action, such as `ClearCollect(‘foo’, [])`
4. Now delete the entire test, which should happen successfully
5. Save the app to local workspace and then unpack

The app should now unpack successfully without any errors or warnings associated with Tests.

## Error PA3013: Property Value Changed: TopParent.AllowAccessToGlobals (or similar)

This can occur if the AllowAccessToGlobals field (associated with the Access app scope property for a custom component) within the msapp's Components json is not in sync with the same field in the ComponentsMetadata.json file. This can be resolved by the following steps -

1. Open the msapp back into the Power Apps Studio
2. Select the affected custom component
3. Toggle the Access app scope property twice i.e. flip the property once and flip it back to its original state
4. Save and download this revised app back to your workspace
5. Unpack this new msapp

The msapp should now unpack successfully without errors.

## Warning PA2001: Checksum mismatch. File Controls\198.json checksum does not match on extract (or similar) \n Error   PA3013: Property Value Changed: TopParent.Children[4].Children[0].Rules[4].InvariantScript (or similar)

This can occur if the function property InvariantScript value of controls with component definition set to false is not in sync with the same field in the component definition. This can be resolved by the following steps -

1. Open the msapp back into the Power Apps Studio
2. Save and download this app back to your workspace
3. Unpack this new msapp

The msapp should now unpack successfully without errors.
