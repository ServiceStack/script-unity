﻿using System;
using System.Collections.Generic;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.service;
using Unity.UIWidgets.ui;
using UnityEngine;
using Canvas = Unity.UIWidgets.ui.Canvas;
using Color = Unity.UIWidgets.ui.Color;
using Rect = Unity.UIWidgets.ui.Rect;

namespace Unity.UIWidgets.rendering {
    public delegate void SelectionChangedHandler(TextSelection selection, RenderEditable renderObject,
        SelectionChangedCause cause);

    public delegate void CaretChangedHandler(Rect caretRect);

    public enum SelectionChangedCause {
        tap,
        doubleTap,
        longPress,
        forcePress,
        keyboard,
        drag
    }

    public class TextSelectionPoint {
        public readonly Offset point;
        public readonly TextDirection? direction;

        public TextSelectionPoint(Offset point, TextDirection? direction) {
            D.assert(point != null);
            this.point = point;
            this.direction = direction;
        }

        public override string ToString() {
            return $"Point: {this.point}, Direction: {this.direction}";
        }
    }

    public class RenderEditable : RenderBox {
        public static readonly char obscuringCharacter = '•';
        static readonly float _kCaretGap = 1.0f;
        static readonly float _kCaretHeightOffset = 2.0f;
        static readonly Offset _kFloatingCaretSizeIncrease = new Offset(0.5f, 1.0f);
        static readonly float _kFloatingCaretRadius = 1.0f;

        TextPainter _textPainter;
        Color _cursorColor;
        int? _maxLines;
        Color _selectionColor;
        ViewportOffset _offset;
        ValueNotifier<bool> _showCursor;
        TextSelection _selection;
        bool _obscureText;
        TapGestureRecognizer _tap;
        LongPressGestureRecognizer _longPress;
        DoubleTapGestureRecognizer _doubleTap;
        public bool ignorePointer;
        public SelectionChangedHandler onSelectionChanged;
        public CaretChangedHandler onCaretChanged;
        Rect _lastCaretRect;
        float? _textLayoutLastWidth;
        List<TextBox> _selectionRects;
        Rect _caretPrototype;
        bool _hasVisualOverflow = false;
        Offset _lastTapDownPosition;

        public RenderEditable(
            TextSpan text,
            TextDirection textDirection,
            ViewportOffset offset,
            ValueNotifier<bool> showCursor,
            TextAlign textAlign = TextAlign.left,
            float textScaleFactor = 1.0f,
            Color cursorColor = null,
            Color backgroundCursorColor = null,
            bool? hasFocus = null,
            int? maxLines = 1,
            Color selectionColor = null,
            TextSelection selection = null,
            bool obscureText = false,
            SelectionChangedHandler onSelectionChanged = null,
            CaretChangedHandler onCaretChanged = null,
            bool ignorePointer = false,
            float cursorWidth = 1.0f,
            Radius cursorRadius = null,
            bool? enableInteractiveSelection = null,
            bool paintCursorAboveText = false,
            Offset cursorOffset = null,
            float devicePixelRatio = 1.0f,
            EdgeInsets floatingCursorAddedMargin = null,
            TextSelectionDelegate textSelectionDelegate = null) {
            floatingCursorAddedMargin = floatingCursorAddedMargin ?? EdgeInsets.fromLTRB(4, 4, 4, 5);
            D.assert(textSelectionDelegate != null);
            this._textPainter = new TextPainter(text: text, textAlign: textAlign, textDirection: textDirection,
                textScaleFactor: textScaleFactor);
            this._cursorColor = cursorColor;

            this._showCursor = showCursor ?? new ValueNotifier<bool>(false);
            this._hasFocus = hasFocus ?? false;
            this._maxLines = maxLines;
            this._selectionColor = selectionColor;
            this._selection = selection;
            this._obscureText = obscureText;
            this._offset = offset;
            this._cursorWidth = cursorWidth;
            this._cursorRadius = cursorRadius;
            this._enableInteractiveSelection = enableInteractiveSelection;
            this.ignorePointer = ignorePointer;
            this.onCaretChanged = onCaretChanged;
            this.onSelectionChanged = onSelectionChanged;
            this.textSelectionDelegate = textSelectionDelegate;

            D.assert(this._maxLines == null || this._maxLines > 0);
            D.assert(this._showCursor != null);
            D.assert(!this._showCursor.value || cursorColor != null);

            this._tap = new TapGestureRecognizer(this);
            this._doubleTap = new DoubleTapGestureRecognizer(this);
            this._tap.onTapDown = this._handleTapDown;
            this._tap.onTap = this._handleTap;
            this._doubleTap.onDoubleTap = this._handleDoubleTap;
            this._longPress = new LongPressGestureRecognizer(debugOwner: this);
            this._longPress.onLongPress = this._handleLongPress;

            this._backgroundCursorColor = backgroundCursorColor;
            this._paintCursorOnTop = paintCursorAboveText;
            this._cursorOffset = cursorOffset;
            this._floatingCursorAddedMargin = floatingCursorAddedMargin;
            this._devicePixelRatio = devicePixelRatio;
        }

        public float devicePixelRatio {
            get { return this._devicePixelRatio; }
            set {
                if (this.devicePixelRatio == value) {
                    return;
                }

                this._devicePixelRatio = value;
                this.markNeedsTextLayout();
            }
        }

        float _devicePixelRatio;

        public Color backgroundCursorColor {
            get { return this._backgroundCursorColor; }
            set {
                if (this.backgroundCursorColor == value) {
                    return;
                }

                this._backgroundCursorColor = value;
                this.markNeedsPaint();
            }
        }

        Color _backgroundCursorColor;

        public bool paintCursorAboveText {
            get { return this._paintCursorOnTop; }
            set {
                if (this._paintCursorOnTop == value) {
                    return;
                }

                this._paintCursorOnTop = value;
                this.markNeedsLayout();
            }
        }

        bool _paintCursorOnTop;

        public Offset cursorOffset {
            get { return this._cursorOffset; }
            set {
                if (this._cursorOffset == value) {
                    return;
                }

                this._cursorOffset = value;
                this.markNeedsLayout();
            }
        }

        Offset _cursorOffset;

        public EdgeInsets floatingCursorAddedMargin {
            get { return this._floatingCursorAddedMargin; }
            set {
                if (this._floatingCursorAddedMargin == value) {
                    return;
                }

                this._floatingCursorAddedMargin = value;
                this.markNeedsPaint();
            }
        }

        EdgeInsets _floatingCursorAddedMargin;

        bool _floatingCursorOn = false;
        Offset _floatingCursorOffset;
        TextPosition _floatingCursorTextPosition;

        public bool selectionEnabled {
            get { return this.enableInteractiveSelection ?? !this.obscureText; }
        }

        public bool obscureText {
            get { return this._obscureText; }
            set {
                if (this._obscureText == value) {
                    return;
                }

                this._obscureText = value;
                this.markNeedsSemanticsUpdate();
            }
        }

        public TextSelectionDelegate textSelectionDelegate;

        int _extentOffset = -1;

        int _baseOffset = -1;

        int _previousCursorLocation;

        bool _resetCursor = false;

        void _handleKeyEvent(RawKeyEvent keyEvent) {
            if (keyEvent is RawKeyUpEvent) {
                return;
            }

            if (this.selection.isCollapsed) {
                this._extentOffset = this.selection.extentOffset;
                this._baseOffset = this.selection.baseOffset;
            }

            KeyCode pressedKeyCode = keyEvent.data.unityEvent.keyCode;
            int modifiers = (int) keyEvent.data.unityEvent.modifiers;
            bool shift = (modifiers & (int) EventModifiers.Shift) > 0;
            bool ctrl = (modifiers & (int) EventModifiers.Control) > 0;
            bool alt = (modifiers & (int) EventModifiers.Alt) > 0;
            bool cmd = (modifiers & (int) EventModifiers.Command) > 0;

            bool rightArrow = pressedKeyCode == KeyCode.RightArrow;
            bool leftArrow = pressedKeyCode == KeyCode.LeftArrow;
            bool upArrow = pressedKeyCode == KeyCode.UpArrow;
            bool downArrow = pressedKeyCode == KeyCode.DownArrow;
            bool arrow = leftArrow || rightArrow || upArrow || downArrow;
            bool aKey = pressedKeyCode == KeyCode.A;
            bool xKey = pressedKeyCode == KeyCode.X;
            bool vKey = pressedKeyCode == KeyCode.V;
            bool cKey = pressedKeyCode == KeyCode.C;
            bool del = pressedKeyCode == KeyCode.Delete;
            bool isMac = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;

            if (keyEvent is RawKeyCommandEvent) {
                // editor case
                this._handleShortcuts(((RawKeyCommandEvent) keyEvent).command);
                return;
            }

            if ((ctrl || (isMac && cmd)) && (xKey || vKey || cKey || aKey)) {
                // runtime case
                if (xKey) {
                    this._handleShortcuts(KeyCommand.Cut);
                }
                else if (aKey) {
                    this._handleShortcuts(KeyCommand.SelectAll);
                }
                else if (vKey) {
                    this._handleShortcuts(KeyCommand.Paste);
                }
                else if (cKey) {
                    this._handleShortcuts(KeyCommand.Copy);
                }

                return;
            }

            if (arrow) {
                int newOffset = this._extentOffset;
                var word = (isMac && alt) || ctrl;
                if (word) {
                    newOffset = this._handleControl(rightArrow, leftArrow, word, newOffset);
                }

                newOffset = this._handleHorizontalArrows(rightArrow, leftArrow, shift, newOffset);
                if (downArrow || upArrow) {
                    newOffset = this._handleVerticalArrows(upArrow, downArrow, shift, newOffset);
                }

                newOffset = this._handleShift(rightArrow, leftArrow, shift, newOffset);

                this._extentOffset = newOffset;
            }

            if (del) {
                this._handleDelete();
            }
        }

        int _handleControl(bool rightArrow, bool leftArrow, bool ctrl, int newOffset) {
            // If control is pressed, we will decide which way to look for a word
            // based on which arrow is pressed.
            if (leftArrow && this._extentOffset > 2) {
                TextSelection textSelection =
                    this._selectWordAtOffset(new TextPosition(offset: this._extentOffset - 2));
                newOffset = textSelection.baseOffset + 1;
            }
            else if (rightArrow && this._extentOffset < this.text.text.Length - 2) {
                TextSelection textSelection =
                    this._selectWordAtOffset(new TextPosition(offset: this._extentOffset + 1));
                newOffset = textSelection.extentOffset - 1;
            }

            return newOffset;
        }

        int _handleHorizontalArrows(bool rightArrow, bool leftArrow, bool shift, int newOffset) {
            if (rightArrow && this._extentOffset < this.text.text.Length) {
                newOffset += 1;
                if (shift) {
                    this._previousCursorLocation += 1;
                }
            }

            if (leftArrow && this._extentOffset > 0) {
                newOffset -= 1;
                if (shift) {
                    this._previousCursorLocation -= 1;
                }
            }

            return newOffset;
        }

        int _handleVerticalArrows(bool upArrow, bool downArrow, bool shift, int newOffset) {
            float plh = this._textPainter.preferredLineHeight;
            float verticalOffset = upArrow ? -0.5f * plh : 1.5f * plh;

            Offset caretOffset =
                this._textPainter.getOffsetForCaret(new TextPosition(offset: this._extentOffset), this._caretPrototype);
            Offset caretOffsetTranslated = caretOffset.translate(0.0f, verticalOffset);
            TextPosition position = this._textPainter.getPositionForOffset(caretOffsetTranslated);

            if (position.offset == this._extentOffset) {
                if (downArrow) {
                    newOffset = this.text.text.Length;
                }
                else if (upArrow) {
                    newOffset = 0;
                }

                this._resetCursor = shift;
            }
            else if (this._resetCursor && shift) {
                newOffset = this._previousCursorLocation;
                this._resetCursor = false;
            }
            else {
                newOffset = position.offset;
                this._previousCursorLocation = newOffset;
            }

            return newOffset;
        }

        int _handleShift(bool rightArrow, bool leftArrow, bool shift, int newOffset) {
            if (this.onSelectionChanged == null) {
                return newOffset;
            }

            if (shift) {
                if (this._baseOffset < newOffset) {
                    this.onSelectionChanged(
                        new TextSelection(
                            baseOffset: this._baseOffset,
                            extentOffset: newOffset
                        ),
                        this,
                        SelectionChangedCause.keyboard
                    );
                }
                else {
                    this.onSelectionChanged(
                        new TextSelection(
                            baseOffset: newOffset,
                            extentOffset: this._baseOffset
                        ),
                        this,
                        SelectionChangedCause.keyboard
                    );
                }
            }
            else {
                if (!this.selection.isCollapsed) {
                    if (leftArrow) {
                        newOffset = this._baseOffset < this._extentOffset ? this._baseOffset : this._extentOffset;
                    }
                    else if (rightArrow) {
                        newOffset = this._baseOffset > this._extentOffset ? this._baseOffset : this._extentOffset;
                    }
                }

                this.onSelectionChanged(
                    TextSelection.fromPosition(
                        new TextPosition(
                            offset: newOffset
                        )
                    ),
                    this,
                    SelectionChangedCause.keyboard
                );
            }

            return newOffset;
        }

        void _handleShortcuts(KeyCommand cmd) {
            switch (cmd) {
                case KeyCommand.Copy:
                    if (!this.selection.isCollapsed) {
                        Clipboard.setData(
                            new ClipboardData(text: this.selection.textInside(this.text.text)));
                    }

                    break;
                case KeyCommand.Cut:
                    if (!this.selection.isCollapsed) {
                        Clipboard.setData(
                            new ClipboardData(text: this.selection.textInside(this.text.text)));
                        this.textSelectionDelegate.textEditingValue = new TextEditingValue(
                            text: this.selection.textBefore(this.text.text)
                                  + this.selection.textAfter(this.text.text),
                            selection: TextSelection.collapsed(offset: this.selection.start)
                        );
                    }

                    break;
                case KeyCommand.Paste:
                    TextEditingValue value = this.textSelectionDelegate.textEditingValue;
                    Clipboard.getData(Clipboard.kTextPlain).Then(data => {
                        if (data != null) {
                            this.textSelectionDelegate.textEditingValue = new TextEditingValue(
                                text: value.selection.textBefore(value.text)
                                      + data.text
                                      + value.selection.textAfter(value.text),
                                selection: TextSelection.collapsed(
                                    offset: value.selection.start + data.text.Length
                                )
                            );
                        }
                    });

                    break;
                case KeyCommand.SelectAll:
                    this._baseOffset = 0;
                    this._extentOffset = this.textSelectionDelegate.textEditingValue.text.Length;
                    this.onSelectionChanged(
                        new TextSelection(
                            baseOffset: 0,
                            extentOffset: this.textSelectionDelegate.textEditingValue.text.Length
                        ),
                        this,
                        SelectionChangedCause.keyboard
                    );
                    break;
                default:
                    D.assert(false);
                    break;
            }
        }

        void _handleDelete() {
            var selection = this.selection;
            if (selection.textAfter(this.text.text).isNotEmpty()) {
                this.textSelectionDelegate.textEditingValue = new TextEditingValue(
                    text: selection.textBefore(this.text.text)
                          + selection.textAfter(this.text.text).Substring(1),
                    selection: TextSelection.collapsed(offset: selection.start)
                );
            }
            else {
                this.textSelectionDelegate.textEditingValue = new TextEditingValue(
                    text: selection.textBefore(this.text.text),
                    selection: TextSelection.collapsed(offset: selection.start)
                );
            }
        }

        protected void markNeedsTextLayout() {
            this._textLayoutLastWidth = null;
            this.markNeedsLayout();
        }

        public TextSpan text {
            get { return this._textPainter.text; }
            set {
                if (this._textPainter.text == value) {
                    return;
                }

                this._textPainter.text = value;
                this.markNeedsTextLayout();
                this.markNeedsSemanticsUpdate();
            }
        }

        public TextAlign textAlign {
            get { return this._textPainter.textAlign; }
            set {
                if (this._textPainter.textAlign == value) {
                    return;
                }

                this._textPainter.textAlign = value;
                this.markNeedsPaint();
            }
        }

        public TextDirection? textDirection {
            get { return this._textPainter.textDirection; }
            set {
                if (this._textPainter.textDirection == value) {
                    return;
                }

                this._textPainter.textDirection = value;
                this.markNeedsTextLayout();
                this.markNeedsSemanticsUpdate();
            }
        }

        public Color cursorColor {
            get { return this._cursorColor; }
            set {
                if (this._cursorColor == value) {
                    return;
                }

                this._cursorColor = value;
                this.markNeedsPaint();
            }
        }

        public ValueNotifier<bool> showCursor {
            get { return this._showCursor; }
            set {
                D.assert(value != null);
                if (this._showCursor == value) {
                    return;
                }

                if (this.attached) {
                    this._showCursor.removeListener(this.markNeedsPaint);
                }

                this._showCursor = value;
                if (this.attached) {
                    this._showCursor.addListener(this.markNeedsPaint);
                }

                this.markNeedsPaint();
            }
        }

        bool _hasFocus;
        bool _listenerAttached = false;

        public bool hasFocus {
            get { return this._hasFocus; }
            set {
                if (this._hasFocus == value) {
                    return;
                }

                this._hasFocus = value;
                if (this._hasFocus) {
                    D.assert(!this._listenerAttached);
                    RawKeyboard.instance.addListener(this._handleKeyEvent);
                    this._listenerAttached = true;
                }
                else {
                    D.assert(this._listenerAttached);
                    RawKeyboard.instance.removeListener(this._handleKeyEvent);
                    this._listenerAttached = false;
                }

                this.markNeedsSemanticsUpdate();
            }
        }

        public int? maxLines {
            get { return this._maxLines; }
            set {
                D.assert(value == null || value > 0);
                if (this._maxLines == value) {
                    return;
                }

                this._maxLines = value;
                this.markNeedsTextLayout();
            }
        }

        public Color selectionColor {
            get { return this._selectionColor; }
            set {
                if (this._selectionColor == value) {
                    return;
                }

                this._selectionColor = value;
                this.markNeedsPaint();
            }
        }

        public float textScaleFactor {
            get { return this._textPainter.textScaleFactor; }
            set {
                if (this._textPainter.textScaleFactor == value) {
                    return;
                }

                this._textPainter.textScaleFactor = value;
                this.markNeedsTextLayout();
            }
        }

        public TextSelection selection {
            get { return this._selection; }
            set {
                if (this._selection == value) {
                    return;
                }

                this._selection = value;
                this._selectionRects = null;
                this.markNeedsPaint();
                this.markNeedsSemanticsUpdate();
            }
        }

        public ViewportOffset offset {
            get { return this._offset; }
            set {
                D.assert(this.offset != null);
                if (this._offset == value) {
                    return;
                }

                if (this.attached) {
                    this._offset.removeListener(this.markNeedsPaint);
                }

                this._offset = value;
                if (this.attached) {
                    this._offset.addListener(this.markNeedsPaint);
                }

                this.markNeedsLayout();
            }
        }

        float _cursorWidth = 1.0f;

        public float cursorWidth {
            get { return this._cursorWidth; }
            set {
                if (this._cursorWidth == value) {
                    return;
                }

                this._cursorWidth = value;
                this.markNeedsLayout();
            }
        }

        Radius _cursorRadius;

        public Radius cursorRadius {
            get { return this._cursorRadius; }
            set {
                if (this._cursorRadius == value) {
                    return;
                }

                this._cursorRadius = value;
                this.markNeedsLayout();
            }
        }

        bool? _enableInteractiveSelection;

        public bool? enableInteractiveSelection {
            get { return this._enableInteractiveSelection; }
            set {
                if (this._enableInteractiveSelection == value) {
                    return;
                }

                this._enableInteractiveSelection = value;
                this.markNeedsTextLayout();
                this.markNeedsSemanticsUpdate();
            }
        }

        public float preferredLineHeight {
            get { return this._textPainter.preferredLineHeight; }
        }


        public override void attach(object ownerObject) {
            base.attach(ownerObject);
            this._offset.addListener(this.markNeedsLayout);
            this._showCursor.addListener(this.markNeedsPaint);
        }

        public override void detach() {
            this._offset.removeListener(this.markNeedsLayout);
            this._showCursor.removeListener(this.markNeedsPaint);
            if (this._listenerAttached) {
                RawKeyboard.instance.removeListener(this._handleKeyEvent);
            }

            base.detach();
        }

        /// Returns the local coordinates of the endpoints of the given selection.
        ///
        /// If the selection is collapsed (and therefore occupies a single point), the
        /// returned list is of length one. Otherwise, the selection is not collapsed
        /// and the returned list is of length two. In this case, however, the two
        /// points might actually be co-located (e.g., because of a bidirectional
        /// selection that contains some text but whose ends meet in the middle).
        ///
        public List<TextSelectionPoint> getEndpointsForSelection(TextSelection selection) {
            D.assert(this.constraints != null);
            this._layoutText(this.constraints.maxWidth);
            var paintOffset = this._paintOffset;
            if (selection.isCollapsed) {
                var caretOffset = this._textPainter.getOffsetForCaret(selection.extendPos, this._caretPrototype);
                var start = new Offset(0.0f, this.preferredLineHeight) + caretOffset + paintOffset;
                return new List<TextSelectionPoint> {new TextSelectionPoint(start, null)};
            }
            else {
                var boxes = this._textPainter.getBoxesForSelection(selection);
                var start = new Offset(boxes[0].start, boxes[0].bottom) + paintOffset;
                var last = boxes.Count - 1;
                var end = new Offset(boxes[last].end, boxes[last].bottom) + paintOffset;
                return new List<TextSelectionPoint> {
                    new TextSelectionPoint(start, boxes[0].direction),
                    new TextSelectionPoint(end, boxes[last].direction),
                };
            }
        }

        public TextPosition getPositionForPoint(Offset globalPosition) {
            this._layoutText(this.constraints.maxWidth);
            globalPosition -= this._paintOffset;
            return this._textPainter.getPositionForOffset(this.globalToLocal(globalPosition));
        }

        public Rect getLocalRectForCaret(TextPosition caretPosition) {
            this._layoutText(this.constraints.maxWidth);
            var caretOffset = this._textPainter.getOffsetForCaret(caretPosition, this._caretPrototype);
            Rect rect = Rect.fromLTWH(0.0f, 0.0f, this.cursorWidth, this.preferredLineHeight)
                .shift(caretOffset + this._paintOffset);
            if (this._cursorOffset != null) {
                rect = rect.shift(this._cursorOffset);
            }

            return rect.shift(this._getPixelPerfectCursorOffset(rect));
        }

        public TextPosition getPositionDown(TextPosition position) {
            return this._textPainter.getPositionVerticalMove(position, 1);
        }

        public TextPosition getPositionUp(TextPosition position) {
            return this._textPainter.getPositionVerticalMove(position, -1);
        }

        public TextPosition getLineStartPosition(TextPosition position, TextAffinity? affinity = null) {
            var line = this._textPainter.getLineRange(position);
            return new TextPosition(offset: line.start, affinity: affinity ?? position.affinity);
        }

        public bool isLineEndOrStart(int offset) {
            int lineCount = this._textPainter.getLineCount();
            for (int i = 0; i < lineCount; i++) {
                var line = this._textPainter.getLineRange(i);
                if (line.start == offset || line.endIncludingNewLine == offset) {
                    return true;
                }
            }

            return false;
        }

        public TextPosition getLineEndPosition(TextPosition position, TextAffinity? affinity = null) {
            var line = this._textPainter.getLineRange(position);
            return new TextPosition(offset: line.endIncludingNewLine, affinity: affinity ?? position.affinity);
        }

        public TextPosition getWordRight(TextPosition position) {
            return this._textPainter.getWordRight(position);
        }

        public TextPosition getWordLeft(TextPosition position) {
            return this._textPainter.getWordLeft(position);
        }

        public TextPosition getParagraphStart(TextPosition position, TextAffinity? affinity = null) {
            D.assert(!this._needsLayout);
            int lineIndex = this._textPainter.getLineIndex(position);
            while (lineIndex - 1 >= 0) {
                var preLine = this._textPainter.getLineRange(lineIndex - 1);
                if (preLine.hardBreak) {
                    break;
                }

                lineIndex--;
            }

            var line = this._textPainter.getLineRange(lineIndex);
            return new TextPosition(offset: line.start, affinity: affinity ?? position.affinity);
        }

        public TextPosition getParagraphEnd(TextPosition position, TextAffinity? affinity = null) {
            D.assert(!this._needsLayout);
            int lineIndex = this._textPainter.getLineIndex(position);
            int maxLine = this._textPainter.getLineCount();
            while (lineIndex < maxLine) {
                var line = this._textPainter.getLineRange(lineIndex);
                if (line.hardBreak) {
                    break;
                }

                lineIndex++;
            }

            return new TextPosition(offset: this._textPainter.getLineRange(lineIndex).endIncludingNewLine,
                affinity: affinity ?? position.affinity);
        }

        public TextPosition getParagraphForward(TextPosition position, TextAffinity? affinity = null) {
            var lineCount = this._textPainter.getLineCount();
            Paragraph.LineRange line = null;
            for (int i = 0; i < lineCount; ++i) {
                line = this._textPainter.getLineRange(i);
                if (!line.hardBreak) {
                    continue;
                }

                if (line.end > position.offset) {
                    break;
                }
            }

            if (line == null) {
                return new TextPosition(position.offset, affinity ?? position.affinity);
            }

            return new TextPosition(line.end, affinity ?? position.affinity);
        }


        public TextPosition getParagraphBackward(TextPosition position, TextAffinity? affinity = null) {
            var lineCount = this._textPainter.getLineCount();
            
            Paragraph.LineRange line = null;
            for (int i = lineCount - 1; i >= 0; --i) {
                line = this._textPainter.getLineRange(i);
                if (i != 0 && !this._textPainter.getLineRange(i - 1).hardBreak) {
                    continue;
                }

                if (line.start < position.offset) {
                    break;
                }
            }

            if (line == null) {
                return new TextPosition(position.offset, affinity ?? position.affinity);
            }

            return new TextPosition(line.start, affinity ?? position.affinity);
        }

        protected override float computeMinIntrinsicWidth(float height) {
            this._layoutText(float.PositiveInfinity);
            return this._textPainter.minIntrinsicWidth;
        }

        protected override float computeMaxIntrinsicWidth(float height) {
            this._layoutText(float.PositiveInfinity);
            return this._textPainter.maxIntrinsicWidth + this.cursorWidth;
        }

        protected override float computeMinIntrinsicHeight(float width) {
            return this._preferredHeight(width);
        }

        protected override float computeMaxIntrinsicHeight(float width) {
            return this._preferredHeight(width);
        }

        protected override float? computeDistanceToActualBaseline(TextBaseline baseline) {
            this._layoutText(this.constraints.maxWidth);
            return this._textPainter.computeDistanceToActualBaseline(baseline);
        }

        public override void handleEvent(PointerEvent evt, HitTestEntry entry) {
            if (this.ignorePointer) {
                return;
            }

            D.assert(this.debugHandleEvent(evt, entry));
            if (evt is PointerDownEvent && this.onSelectionChanged != null) {
                this._tap.addPointer((PointerDownEvent) evt);
                this._doubleTap.addPointer((PointerDownEvent) evt);
                this._longPress.addPointer((PointerDownEvent) evt);
            }
        }

        public void handleTapDown(TapDownDetails details) {
            this._lastTapDownPosition = details.globalPosition - this._paintOffset;
            if (!Application.isMobilePlatform) {
                this.selectPosition(SelectionChangedCause.tap);
            }
        }

        void _handleTapDown(TapDownDetails details) {
            D.assert(!this.ignorePointer);
            this.handleTapDown(details);
        }

        public void handleTap() {
            this.selectPosition(cause: SelectionChangedCause.tap);
        }

        void _handleTap() {
            D.assert(!this.ignorePointer);
            this.handleTap();
        }

        void _handleDoubleTap(DoubleTapDetails details) {
            D.assert(!this.ignorePointer);
            this.handleDoubleTap(details);
        }

        public void handleDoubleTap(DoubleTapDetails details) {
            // need set _lastTapDownPosition, otherwise it would be last single tap position
            this._lastTapDownPosition = details.firstGlobalPosition - this._paintOffset;
            this.selectWord(cause: SelectionChangedCause.doubleTap);
        }

        void _handleLongPress() {
            D.assert(!this.ignorePointer);
            this.handleLongPress();
        }

        public void handleLongPress() {
            this.selectWord(cause: SelectionChangedCause.longPress);
        }

        public void selectPositionAt(Offset from = null, Offset to = null, SelectionChangedCause? cause = null) {
            D.assert(cause != null);
            D.assert(from != null);

            this._layoutText(this.constraints.maxWidth);
            if (this.onSelectionChanged != null) {
                TextPosition fromPosition =
                    this._textPainter.getPositionForOffset(this.globalToLocal(from - this._paintOffset));
                TextPosition toPosition = to == null
                    ? null
                    : this._textPainter.getPositionForOffset(this.globalToLocal(to - this._paintOffset));

                int baseOffset = fromPosition.offset;
                int extentOffset = fromPosition.offset;
                if (toPosition != null) {
                    baseOffset = Mathf.Min(fromPosition.offset, toPosition.offset);
                    extentOffset = Mathf.Max(fromPosition.offset, toPosition.offset);
                }

                TextSelection newSelection = new TextSelection(
                    baseOffset: baseOffset,
                    extentOffset: extentOffset,
                    affinity: fromPosition.affinity);

                if (newSelection != this._selection) {
                    this.onSelectionChanged(newSelection, this, cause.Value);
                }
            }
        }

        void selectPosition(SelectionChangedCause? cause = null) {
            this.selectPositionAt(from: this._lastTapDownPosition, cause: cause);
        }

        void selectWord(SelectionChangedCause? cause = null) {
            this.selectWordsInRange(from: this._lastTapDownPosition, cause: cause);
        }

        void selectWordsInRange(Offset from = null, Offset to = null, SelectionChangedCause? cause = null) {
            D.assert(cause != null);
            D.assert(from != null);
            this._layoutText(this.constraints.maxWidth);
            if (this.onSelectionChanged != null) {
                TextPosition firstPosition =
                    this._textPainter.getPositionForOffset(this.globalToLocal(from - this._paintOffset));
                TextSelection firstWord = this._selectWordAtOffset(firstPosition);
                TextSelection lastWord = to == null
                    ? firstWord
                    : this._selectWordAtOffset(
                        this._textPainter.getPositionForOffset(this.globalToLocal(to - this._paintOffset)));

                this.onSelectionChanged(
                    new TextSelection(
                        baseOffset: firstWord.baseOffset,
                        extentOffset: lastWord.extentOffset,
                        affinity: firstWord.affinity),
                    this,
                    cause.Value);
            }
        }

        void selectWordEdge(SelectionChangedCause cause) {
            this._layoutText(this.constraints.maxWidth);
            D.assert(this._lastTapDownPosition != null);
            if (this.onSelectionChanged != null) {
                TextPosition position =
                    this._textPainter.getPositionForOffset(this.globalToLocal(this._lastTapDownPosition));
                TextRange word = this._textPainter.getWordBoundary(position);
                if (position.offset - word.start <= 1) {
                    this.onSelectionChanged(
                        TextSelection.collapsed(offset: word.start, affinity: TextAffinity.downstream),
                        this,
                        cause
                    );
                }
                else {
                    this.onSelectionChanged(
                        TextSelection.collapsed(offset: word.end, affinity: TextAffinity.upstream),
                        this,
                        cause
                    );
                }
            }
        }

        TextSelection _selectWordAtOffset(TextPosition position) {
            D.assert(this._textLayoutLastWidth == this.constraints.maxWidth);
            var word = this._textPainter.getWordBoundary(position);
            if (position.offset >= word.end) {
                return TextSelection.fromPosition(position);
            }

            return new TextSelection(baseOffset: word.start, extentOffset: word.end);
        }

        void _layoutText(float constraintWidth) {
            if (this._textLayoutLastWidth == constraintWidth) {
                return;
            }

            var caretMargin = _kCaretGap + this.cursorWidth;
            var avialableWidth = Mathf.Max(0.0f, constraintWidth - caretMargin);
            var maxWidth = this._isMultiline ? avialableWidth : float.PositiveInfinity;
            this._textPainter.layout(minWidth: avialableWidth, maxWidth: maxWidth);
            this._textLayoutLastWidth = constraintWidth;
        }

        Rect _getCaretPrototype {
            get {
                switch (Application.platform) {
                    case RuntimePlatform.IPhonePlayer:
                        return Rect.fromLTWH(0.0f, -_kCaretHeightOffset + 0.5f, this.cursorWidth,
                            this.preferredLineHeight + 2.0f);
                    default:
                        return Rect.fromLTWH(0.0f, _kCaretHeightOffset, this.cursorWidth,
                            this.preferredLineHeight - 2.0f * _kCaretHeightOffset);
                }
            }
        }


        protected override void performLayout() {
            this._layoutText(this.constraints.maxWidth);
            this._caretPrototype = this._getCaretPrototype;
            this._selectionRects = null;

            var textPainterSize = this._textPainter.size;
            this.size = new Size(this.constraints.maxWidth,
                this.constraints.constrainHeight(this._preferredHeight(this.constraints.maxWidth)));
            var contentSize = new Size(textPainterSize.width + _kCaretGap + this.cursorWidth,
                textPainterSize.height);
            var _maxScrollExtent = this._getMaxScrollExtend(contentSize);
            this._hasVisualOverflow = _maxScrollExtent > 0.0;
            this.offset.applyViewportDimension(this._viewportExtend);
            this.offset.applyContentDimensions(0.0f, _maxScrollExtent);
        }

        public override void paint(PaintingContext context, Offset offset) {
            this._layoutText(this.constraints.maxWidth);
            if (this._hasVisualOverflow) {
                context.pushClipRect(this.needsCompositing, offset, Offset.zero & this.size, this._paintContents);
            }
            else {
                this._paintContents(context, offset);
            }
        }

        protected override bool hitTestSelf(Offset position) {
            return true;
        }

        // describeSemanticsConfiguration todo

        Offset _getPixelPerfectCursorOffset(Rect caretRect) {
            Offset caretPosition = this.localToGlobal(caretRect.topLeft);
            float pixelMultiple = 1.0f / this._devicePixelRatio;
            int quotientX = (caretPosition.dx / pixelMultiple).round();
            int quotientY = (caretPosition.dy / pixelMultiple).round();
            float pixelPerfectOffsetX = quotientX * pixelMultiple - caretPosition.dx;
            float pixelPerfectOffsetY = quotientY * pixelMultiple - caretPosition.dy;
            return new Offset(pixelPerfectOffsetX, pixelPerfectOffsetY);
        }

        void _paintCaret(Canvas canvas, Offset effectiveOffset, TextPosition textPosition) {
            D.assert(this._textLayoutLastWidth == this.constraints.maxWidth);
            var caretOffset = this._textPainter.getOffsetForCaret(textPosition, this._caretPrototype);
            var paint = new Paint() {color = this._floatingCursorOn ? this.backgroundCursorColor : this._cursorColor};
            Rect caretRect = this._caretPrototype.shift(caretOffset + effectiveOffset);
            if (this._cursorOffset != null) {
                caretRect = caretRect.shift(this._cursorOffset);
            }

            if (this.cursorRadius == null) {
                canvas.drawRect(caretRect, paint);
            }
            else {
                RRect caretRRect = RRect.fromRectAndRadius(caretRect, this.cursorRadius);
                canvas.drawRRect(caretRRect, paint);
            }

            if (!caretRect.Equals(this._lastCaretRect)) {
                this._lastCaretRect = caretRect;
                if (this.onCaretChanged != null) {
                    this.onCaretChanged(caretRect);
                }
            }
        }

        public void setFloatingCursor(FloatingCursorDragState? state, Offset boundedOffset, TextPosition lastTextPosition,
            float? resetLerpValue = null) {
            D.assert(boundedOffset != null);
            D.assert(lastTextPosition != null);
            if (state == FloatingCursorDragState.Start) {
                this._relativeOrigin = new Offset(0, 0);
                this._previousOffset = null;
                this._resetOriginOnBottom = false;
                this._resetOriginOnTop = false;
                this._resetOriginOnRight = false;
                this._resetOriginOnBottom = false;
            }

            this._floatingCursorOn = state != FloatingCursorDragState.End;
            this._resetFloatingCursorAnimationValue = resetLerpValue;
            if (this._floatingCursorOn) {
                this._floatingCursorOffset = boundedOffset;
                this._floatingCursorTextPosition = lastTextPosition;
            }

            this.markNeedsPaint();
        }

        void _paintFloatingCaret(Canvas canvas, Offset effectiveOffset) {
            D.assert(this._textLayoutLastWidth == this.constraints.maxWidth);
            D.assert(this._floatingCursorOn);

            Paint paint = new Paint() {color = this._cursorColor.withOpacity(0.75f)};

            float sizeAdjustmentX = _kFloatingCaretSizeIncrease.dx;
            float sizeAdjustmentY = _kFloatingCaretSizeIncrease.dy;

            if (this._resetFloatingCursorAnimationValue != null) {
                sizeAdjustmentX =
                    MathUtils.lerpFloat(sizeAdjustmentX, 0f, this._resetFloatingCursorAnimationValue.Value);
                sizeAdjustmentY =
                    MathUtils.lerpFloat(sizeAdjustmentY, 0f, this._resetFloatingCursorAnimationValue.Value);
            }

            Rect floatingCaretPrototype = Rect.fromLTRB(
                this._caretPrototype.left - sizeAdjustmentX,
                this._caretPrototype.top - sizeAdjustmentY,
                this._caretPrototype.right + sizeAdjustmentX,
                this._caretPrototype.bottom + sizeAdjustmentY
            );

            Rect caretRect = floatingCaretPrototype.shift(effectiveOffset);
            Radius floatingCursorRadius = Radius.circular(_kFloatingCaretRadius);
            RRect caretRRect = RRect.fromRectAndRadius(caretRect, floatingCursorRadius);
            canvas.drawRRect(caretRRect, paint);
        }

        Offset _relativeOrigin = new Offset(0f, 0f);
        Offset _previousOffset;
        bool _resetOriginOnLeft = false;
        bool _resetOriginOnRight = false;
        bool _resetOriginOnTop = false;
        bool _resetOriginOnBottom = false;
        float? _resetFloatingCursorAnimationValue;

        public Offset calculateBoundedFloatingCursorOffset(Offset rawCursorOffset) {
            Offset deltaPosition = new Offset(0f, 0f);
            float topBound = -this.floatingCursorAddedMargin.top;
            float bottomBound = this._textPainter.height - this.preferredLineHeight +
                                this.floatingCursorAddedMargin.bottom;
            float leftBound = -this.floatingCursorAddedMargin.left;
            float rightBound = this._textPainter.width + this.floatingCursorAddedMargin.right;

            if (this._previousOffset != null) {
                deltaPosition = rawCursorOffset - this._previousOffset;
            }

            if (this._resetOriginOnLeft && deltaPosition.dx > 0) {
                this._relativeOrigin = new Offset(rawCursorOffset.dx - leftBound, this._relativeOrigin.dy);
                this._resetOriginOnLeft = false;
            }
            else if (this._resetOriginOnRight && deltaPosition.dx < 0) {
                this._relativeOrigin = new Offset(rawCursorOffset.dx - rightBound, this._relativeOrigin.dy);
                this._resetOriginOnRight = false;
            }

            if (this._resetOriginOnTop && deltaPosition.dy > 0) {
                this._relativeOrigin = new Offset(this._relativeOrigin.dx, rawCursorOffset.dy - topBound);
                this._resetOriginOnTop = false;
            }
            else if (this._resetOriginOnBottom && deltaPosition.dy < 0) {
                this._relativeOrigin = new Offset(this._relativeOrigin.dx, rawCursorOffset.dy - bottomBound);
                this._resetOriginOnBottom = false;
            }

            float currentX = rawCursorOffset.dx - this._relativeOrigin.dx;
            float currentY = rawCursorOffset.dy - this._relativeOrigin.dy;
            float adjustedX = Mathf.Min(Mathf.Max(currentX, leftBound), rightBound);
            float adjustedY = Mathf.Min(Mathf.Max(currentY, topBound), bottomBound);
            Offset adjustedOffset = new Offset(adjustedX, adjustedY);

            if (currentX < leftBound && deltaPosition.dx < 0) {
                this._resetOriginOnLeft = true;
            }
            else if (currentX > rightBound && deltaPosition.dx > 0) {
                this._resetOriginOnRight = true;
            }

            if (currentY < topBound && deltaPosition.dy < 0) {
                this._resetOriginOnTop = true;
            }
            else if (currentY > bottomBound && deltaPosition.dy > 0) {
                this._resetOriginOnBottom = true;
            }

            this._previousOffset = rawCursorOffset;

            return adjustedOffset;
        }

        void _paintSelection(Canvas canvas, Offset effectiveOffset) {
            D.assert(this._textLayoutLastWidth == this.constraints.maxWidth);
            D.assert(this._selectionRects != null);
            var paint = new Paint() {color = this._selectionColor};

            foreach (var box in this._selectionRects) {
                canvas.drawRect(box.toRect().shift(effectiveOffset), paint);
            }
        }

        void _paintContents(PaintingContext context, Offset offset) {
            D.assert(this._textLayoutLastWidth == this.constraints.maxWidth);
            var effectiveOffset = offset + this._paintOffset;

            bool showSelection = false;
            bool showCaret = false;

            if (this._selection != null && !this._floatingCursorOn) {
                if (this._selection.isCollapsed && this._showCursor.value && this.cursorColor != null) {
                    showCaret = true;
                }
                else if (!this._selection.isCollapsed && this._selectionColor != null) {
                    showSelection = true;
                }
            }

            if (showSelection) {
                this._selectionRects = this._selectionRects ?? this._textPainter.getBoxesForSelection(this._selection);
                this._paintSelection(context.canvas, effectiveOffset);
            }

            if (this.paintCursorAboveText) {
                this._textPainter.paint(context.canvas, effectiveOffset);
            }

            if (showCaret) {
                this._paintCaret(context.canvas, effectiveOffset, this._selection.extendPos);
            }

            if (!this.paintCursorAboveText) {
                this._textPainter.paint(context.canvas, effectiveOffset);
            }

            if (this._floatingCursorOn) {
                if (this._resetFloatingCursorAnimationValue == null) {
                    this._paintCaret(context.canvas, effectiveOffset, this._floatingCursorTextPosition);
                }

                this._paintFloatingCaret(context.canvas, this._floatingCursorOffset);
            }
        }

        void markNeedsSemanticsUpdate() {
            // todo
        }

        float _preferredHeight(float width) {
            if (this.maxLines != null) {
                return this.preferredLineHeight * this.maxLines.Value;
            }

            if (!width.isFinite()) {
                var text = this._textPainter.text.text;
                int lines = 1;
                for (int index = 0; index < text.Length; ++index) {
                    if (text[index] == 0x0A) {
                        lines += 1;
                    }
                }

                return this.preferredLineHeight * lines;
            }

            this._layoutText(width);
            return Mathf.Max(this.preferredLineHeight, this._textPainter.height);
        }

        bool _isMultiline {
            get { return this._maxLines != 1; }
        }

        Axis _viewportAxis {
            get { return this._isMultiline ? Axis.vertical : Axis.horizontal; }
        }

        Offset _paintOffset {
            get {
                switch (this._viewportAxis) {
                    case Axis.horizontal:
                        return new Offset(-this.offset.pixels, 0.0f);
                    case Axis.vertical:
                        return new Offset(0.0f, -this.offset.pixels);
                }

                return null;
            }
        }

        float _viewportExtend {
            get {
                D.assert(this.hasSize);
                switch (this._viewportAxis) {
                    case Axis.horizontal:
                        return this.size.width;
                    case Axis.vertical:
                        return this.size.height;
                }

                return 0.0f;
            }
        }

        float _getMaxScrollExtend(Size contentSize) {
            D.assert(this.hasSize);
            switch (this._viewportAxis) {
                case Axis.horizontal:
                    return Mathf.Max(0.0f, contentSize.width - this.size.width);
                case Axis.vertical:
                    return Mathf.Max(0.0f, contentSize.height - this.size.height);
            }

            return 0.0f;
        }

        public override Rect describeApproximatePaintClip(RenderObject child) {
            return this._hasVisualOverflow ? Offset.zero & this.size : null;
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.debugFillProperties(properties);
            properties.add(new DiagnosticsProperty<Color>("cursorColor", this.cursorColor));
            properties.add(new DiagnosticsProperty<ValueNotifier<bool>>("showCursor", this.showCursor));
            properties.add(new DiagnosticsProperty<int?>("maxLines", this.maxLines));
            properties.add(new DiagnosticsProperty<Color>("selectionColor", this.selectionColor));
            properties.add(new DiagnosticsProperty<float>("textScaleFactor", this.textScaleFactor));
            properties.add(new DiagnosticsProperty<TextSelection>("selection", this.selection));
            properties.add(new DiagnosticsProperty<ViewportOffset>("offset", this.offset));
        }

        public override List<DiagnosticsNode> debugDescribeChildren() {
            return new List<DiagnosticsNode> {
                this.text.toDiagnosticsNode(
                    name: "text",
                    style: DiagnosticsTreeStyle.transition
                ),
            };
        }
    }
}