# Third-Party Notices

This file records third-party material incorporated directly into source code.
It is provenance information, not a legal-compliance certification and not a
replacement for the governing repository and upstream licenses.

## SixLabors.Fonts

`src/FrankenTui.Text/BidiEngine.cs` adapts the Unicode Bidirectional Algorithm
engine from SixLabors.Fonts v1.0.0 at commit
`32bef42997adb10268369ca149777f00e4241ce9`.

Copyright (c) Six Labors.

Licensed under the Apache License, Version 2.0. A copy is provided in
`LICENSES/Apache-2.0.txt`. The managed file is a modified adaptation and
retains an SPDX identifier and source coordinate in its header.

## unicode-bidi

`src/FrankenTui.Text/Bidi.cs` contains a managed projection of Unicode 16.0
property and bracket data from `unicode-bidi` 0.3.18. The pinned Cargo package
checksum is
`5c1cb5db39152898a79168971543b1cb5020dff7fe43c8dc468b0885f5e29df5`.

Copyright 2015, The Servo Project Developers (see the upstream AUTHORS file).

The upstream crate is offered under Apache-2.0 or MIT terms. This repository's
projected file selects Apache-2.0, as recorded by its SPDX identifier; a copy is
provided in `LICENSES/Apache-2.0.txt`.
