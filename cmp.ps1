param([int]$screen=2, [int]$w=80, [int]$h=24)
dotnet run --project tools/FrankenTui.SideBySide -- $screen $w $h
