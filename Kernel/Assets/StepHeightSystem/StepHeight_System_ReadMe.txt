StepHeightController System - README

Overview

The StepHeightController is a component designed to handle a player's ability to step over obstacles of varying heights in a smooth and controlled manner. It detects nearby obstacles and smoothly moves the player over them if the conditions for stepping are met. This system is highly configurable and can be used in combination with your own movement controllers, or the included MovementController demo, to create a seamless player movement experience.

Features
    - Step Detection: Automatically detects nearby steps or obstacles that the player can step over.
    - Smooth Step Motion: Smoothly lifts the player over obstacles within a configurable range.
    - Customizable Settings: The maximum step height, step angle threshold, and step smoothness can all be customized via the inspector or programmatically.
    - Debugging Tools: Includes options to visualize step detection and movement in the Unity scene view.
    - Collision Handling: Works with Unity's physics system to ensure correct handling of obstacles and collisions.


How It Works

The StepHeightController operates by:
    1. Detecting nearby obstacles using overlap spheres and raycasts.
    2. Calculating if the player can step up onto the obstacle based on its height, distance, and angle relative to the player's movement direction.
    3. Moving the player smoothly over the obstacle if all conditions are met.

This process is repeated in real-time as the player moves, ensuring that they can navigate steps or small ledges seamlessly without manual jump input.


How to Use

1. Adding the StepHeightController

To use the StepHeightController in your game:
    1. Attach the StepHeightController to your player GameObject:
        - In the Unity Editor, select your player GameObject.
        - Click on "Add Component" and search for StepHeightController.
        - Add the component to the player.
    2. Configure the StepHeightController:
        - Adjust the Step Height, Step Up Smooth Factor, and Step Up Angle Threshold to fit your desired player stepping behavior.
        - Enable or disable debugging options in the "Debug" section to visualize how steps are detected in the scene view.

2. Using with Custom Movement Controllers

The StepHeightController can be used alongside your own custom movement logic.
You can integrate it by simply calling CheckForStep() within your movement controller's logic, usually in the update or movement function,
whenever your player is grounded and moving. Here's an example:

private void DoMovement()
{
    if (isGrounded && hasMovementInput)
    {
        stepHeightController.CheckForStep();  // Integrate the step detection
    }

    // Your custom movement logic here
}


3. Using with the Demo MovementController

If you want to quickly test out the stepping feature without implementing your own movement controller, you can use the provided MovementController demo.
The MovementController automatically integrates the StepHeightController and provides basic player movement, rotation, and jumping.
    1. Attach the MovementController and StepHeightController components to your player GameObject.
    2. Configure both controllers as needed (step height, movement speed, etc.).


4. Debugging

To help visualize the step detection process in the editor:
    - Enable Debug Visualization in the StepHeightController inspector.
    - Use Debug Logs for detailed information about step detection and movement in the console.


Key Configurable Settings
Step Settings
    - Step Height: Maximum height that the player can step up. (Default: 0.5)
    - Step Up Smooth Factor: How smoothly the player steps up onto obstacles. Higher values make the movement smoother and slower. (Default: 4.5)
    - Step Up Angle Threshold: Maximum angle between the player’s movement direction and the step direction. If the angle is too large, the player won’t step up. (Default: 65°)

Debug Options
    - Debug Logs: Enable/disable logging of debug messages.
    - Debug Warnings: Enable/disable logging of warnings.
    - Debug Errors: Enable/disable error logs.
    - Debug Visualization: Visualize the step detection process in the Unity scene view.

Example
Here is an example configuration for the StepHeightController:

StepHeightController:
  Step Height: 0.5
  Step Up Smooth Factor: 4.5
  Step Up Angle Threshold: 65
  Layers To Ignore: Player (Layer)
  Debug Log: false
  Debug Visualization: true

This configuration allows the player to step up to obstacles that are up to 0.5 units high, with a smooth stepping motion.
Debug visualizations will be shown in the scene view to help with understanding how the step detection works.


Best Practices
    - Tuning Step Settings: Test different Step Height and Step Up Smooth Factor values to match the style of movement you want. For example, larger characters may need higher step heights, while more agile characters may require quicker step motions.
    - Avoid Overlapping Movements: Ensure that your custom movement controller doesn’t override or conflict with the StepHeightController's movement when a step is detected.
    - Grounded Check: Always ensure your player is grounded before calling CheckForStep() to prevent unexpected behavior in mid-air.

Limitations
    - The StepHeightController is designed for small to medium steps (e.g., stairs, ledges). It is not intended for climbing large obstacles or complex parkour-style movement.
    - The system relies on Unity's physics engine and may need adjustments depending on the player’s collider setup, especially for custom or non-standard character rigs.

Conclusion
    - The StepHeightController provides a robust and flexible way to handle stepping over obstacles in your game. Whether you're using it with the provided demo MovementController or integrating it into your custom system, it can be a powerful tool to create smooth, natural player movement across uneven terrain.

Feel free to modify the settings and experiment with the configurations to best suit your game's movement requirements!