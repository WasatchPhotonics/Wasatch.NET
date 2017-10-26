# Overview

Maintenance notes for ongoing development of Wasatch.NET.

# Properties-vs-Functions

At the moment, I am sticking with functions for most calls because:

- easier to port / align with other languages that don't have complex
  property accessors (especially C)
- allows success/failure result on setters
- it already is that way
