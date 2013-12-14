#include "Example.h"

Example game;
char const * sceneDir = "res/scenes/";

Example::Example()
    : scene(nullptr)
    , font(nullptr)
    , sceneIndex(0)
{
}

void Example::initialize()
{
    font = Font::create("res/arial.gpb");
    GP_ASSERT(font);
    LoadScene();
}

void Example::LoadScene()
{
    // Get all the scene file names in res/scenes
    sceneList.clear();
    FileSystem::listFiles(sceneDir, sceneList);

    if (!sceneList.empty())
    {
        SAFE_RELEASE(scene);
        
        // Wrap the scene index between 0 and the max number of scenes
        int const maxSceneIndex = static_cast<int>(sceneList.size()) - 1;
        sceneIndex = sceneIndex > maxSceneIndex ? 0 : sceneIndex < 0 ? maxSceneIndex : sceneIndex;

        // Load the selected scene
        scene = Scene::load((sceneDir + sceneList.at(sceneIndex)).c_str());
        
        // Set the scenes camera to be the one exported from Unity3D, this will always be in a
        // node named 'Camera'
        Node * cameraNode = scene->findNode("Camera");
        GP_ASSERT(cameraNode && cameraNode->getCamera());
        scene->setActiveCamera(cameraNode->getCamera());
    }
}

void Example::finalize()
{
    SAFE_RELEASE(scene);
    SAFE_RELEASE(font);
}

void Example::render(float elapsedTime)
{
    clear(CLEAR_COLOR_DEPTH, Vector4::zero(), 1.0f, 0);

    int y = 0;
    int const spacing = font->getSize();
    Vector4 const colour(Vector4::one());
    char buffer[255];

    font->start();

    font->drawText("Press Space to reload current scene", 0, y, colour);
    sprintf(buffer, "Press Left/Right arrow to browse scenes in '%s'", sceneDir);
    font->drawText(buffer, 0, y += spacing, colour);

    if (scene)
    {
        getPhysicsController()->drawDebug(scene->getActiveCamera()->getViewProjectionMatrix());
        sprintf(buffer, "Current scene [%d] '%s'", sceneIndex, sceneList.at(sceneIndex).c_str());
        font->drawText(buffer, 0, y += spacing, colour);
    }

    font->finish();
}

void Example::keyEvent(Keyboard::KeyEvent evt, int key)
{
    if (evt == Keyboard::KEY_PRESS)
    {
        switch (key)
        {
        case Keyboard::KEY_ESCAPE:
            exit();
            break;
        case Keyboard::KEY_LEFT_ARROW:
            --sceneIndex;
            LoadScene();
            break;
        case Keyboard::KEY_RIGHT_ARROW:
            ++sceneIndex;
            LoadScene();
            break;
        case Keyboard::KEY_SPACE:
            LoadScene();
            break;
        }
    }
}