# Data Model: Sticky Exercise Column

## Persistence Impact

No database, API, or persisted domain-model changes are required. The feature changes only the presentation of an existing session-detail table.

## Existing Display Entities

### Session Detail Table

Represents the horizontally scrollable table shown for a completed workout in both view and edit modes.

**Existing attributes relevant to this feature**:

- Scroll container
- Header row
- Exercise-name column
- Statistic columns
- Exercise rows
- View or edit presentation mode

### Fixed Exercise Column

Represents the first semantic table column while the table is horizontally scrolled.

**Properties**:

- **Header**: the existing `Exercise` column header
- **Cells**: existing exercise-name cells, one per exercise row
- **Anchor edge**: left edge of the table's scroll viewport
- **Surface**: one opaque light-grey header-row surface shared by the header and body cells
- **Width**: longest displayed exercise name plus cell padding and trailing `var(--spacing-md)` margin; no fixed pixel or percentage width
- **Text flow**: exercise names remain on one line so the column can size to its longest name
- **Layer order**: header above body fixed cells; fixed cells above horizontally moving statistic content
- **Divider**: visual boundary at the fixed column's trailing edge using the existing table border colour; no divider between the header row and first body row

## Invariants

1. The Exercise column remains the first column.
2. The header and body exercise cells use the same horizontal anchor.
3. Exercise names stay in their original rows; no cloned or separately synchronized rows are introduced.
4. Statistic columns retain their existing order, values, widths, and horizontal scrolling behavior.
5. Fixed-cell surfaces are opaque in every supported theme.
6. View mode and edit mode use the same fixed-column behavior.
7. Loading, error, and non-table empty states do not acquire fixed-column state.

## UI State Transitions

| From | Action | To | Required result |
|---|---|---|---|
| Table at left boundary | Scroll right | Table at intermediate/right position | Exercise header and names stay at the left edge; statistics move behind them |
| Table at intermediate/right position | Scroll left | Table at left boundary | Exercise column returns to its original appearance with no offset artifact |
| View mode | Enter edit mode | Edit table | Fixed Exercise column remains active while editable statistic controls scroll |
| Edit mode | Save or cancel | View table | Fixed-column behavior remains active after re-render |
| Light theme | Change to dark/system-dark | Dark table | Fixed surfaces use resolved theme tokens and remain opaque |

## Validation Rules

- The fixed column must not reduce the scroll range needed to reveal the final statistic column.
- The fixed header and first body cell must remain aligned within normal rendering tolerance throughout scrolling.
- The Exercise column width is determined by the longest displayed exercise name plus cell padding; names remain on one line and must not overlap scrolling values.
- No application data is transformed or validated differently by this feature.
