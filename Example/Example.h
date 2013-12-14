#ifndef Example_H_
#define Example_H_

#include "gameplay.h"

using namespace gameplay;

/**
 * Simple example of how you can reload the scenes that you generate using
 * the Unity3D exporter at runtime for rapid iteration.
 */
class Example: public Game
{
public:
    Example();
	void keyEvent(Keyboard::KeyEvent evt, int key);
protected:
    void initialize();
    void finalize();
    void update(float elapsedTime) override {}
    void render(float elapsedTime);
private:
    void LoadScene();
    Scene * scene;
    Font * font;
    int sceneIndex;
    std::vector<std::string> sceneList;
};

#endif
