#!/bin/sh
# Shows every test by name with PASSED / FAILED
dotnet test XsltLab.Tests --logger "console;verbosity=detailed"
