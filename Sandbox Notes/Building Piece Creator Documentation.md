## Opening the Tool
1. In Unity, click on the "Tools" menu at the top
2. Navigate to "Building" → "Building Piece Creator"
3. The Building Piece Creator window will appear
## Creating a New Building Piece

### Step 1: Choose a Template
1. Find an existing building piece you want to use as a starting point
2. Drag that prefab from the Project window into the "Template Prefab" field in the Building Piece Creator window
   - The template should be a prefab that already has a BuildingPiece component
   - When you select a template, the tool will automatically fill in the piece type and suggest a name

### Step 2: Customize Your New Piece
1. **Name**: Change the "Piece Name" if you want something different from the template's name
   - This will be the name of your new prefab
   - Try to use descriptive names like "Stone Wall Corner" or "Wooden Floor"

2. **Piece Type**: Choose what type of building piece this will be
   - Foundation: Base pieces that connect to the ground
   - Wall: Vertical building pieces
   - Floor: Horizontal building pieces
   - Stair: Pieces that connect different height levels
   - The type is pre-selected based on your template, but you can change it if needed

### Step 3: Create the Piece
1. Click the "Create Building Piece" button
2. Your new piece will be automatically created in the correct folder (Assets/Prefabs/Building/[Type])

## Tips
- Always use an existing piece as a template to ensure all components and settings are properly set up
- The tool will automatically organize your new piece in the correct folder based on its type
- If you make a mistake, you can always delete the prefab and try again

## Troubleshooting
- **"Building Piece Database not found!"**: Contact Tim - the database file might be missing
- **"Please assign a template prefab"**: You need to drag an existing building piece prefab into the Template Prefab field
- **Template doesn't work**: Make sure you're using an existing building piece as a template (it needs to have the BuildingPiece component)
