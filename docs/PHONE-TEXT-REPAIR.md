# Phone text rendering repair — September 13, 2026

The live phone displayed its wallpaper, icons and cards but hid all native text.
The previous offline preview painted native text and external textures together in
source order, which did not reproduce the game's two rendering layers.

`CustomSprite` uses ScriptHookV's external texture overlay. Phone labels previously
used GTA's `TextElement`, underneath that overlay. The phone now submits its text,
surfaces and icons through the same external sprite pipeline. This preserves the
existing phone design and navigation while displaying the clock, balances, app
names, row labels, message bodies and confirmation text.

The font consists of 95 baked ASCII glyph textures with matching proportional
advances. Runtime reuses those textures as money, time and messages change; it
does not rasterize text or allocate an ever-growing collection of native textures.
Curly quotes and dashes normalize to readable punctuation. Unsupported characters
use a question mark. No font file is redistributed.

Missing font assets cause the entire phone to use native text and rectangles.
An opaque artwork layer is never combined with fallback native text.

Verification covers actual `FinishFrame` draw submissions, same-layer ordering,
changing balances without additional texture creation, inbox and message content,
and incomplete installations. Runtime draw submissions also generate the preview
images; the earlier preview now shares the exact glyph layout with production.
The production DLL compiles against the pinned SHVDN 3.6.0 reference.

Live checks still needed: open both home pages, Messages and a message detail,
Garage and a confirmation, then scroll a long Journal/Help entry. Confirm labels
remain readable, amounts update, and closing the phone removes its text.

This focused package leaves mission scripts, placements, saves, purchases and
controller bindings unchanged. The larger solo/harbor repair pass remains staged
separately and has not been included in this phone package.

Reference: [SHVDN CustomSprite implementation](https://github.com/scripthookvdotnet/scripthookvdotnet/blob/v3.6.0/source/scripting_v3/GTA.UI/CustomSprite.cs).
