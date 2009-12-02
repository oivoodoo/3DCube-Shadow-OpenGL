
using System;
using System.Collections.Generic;
using System.Drawing;
using Tao.FreeGlut;
using Tao.OpenGl;

namespace Voodoo.Game
{

	public class GameScene
	{
		private static int widthScreen = 640;
		private static int heightScreen = 480;
		private static float[] light = new float[] {30.0f, 90.0f, -30.0f, 1.0f};
		private static double[] clippingPlane = {0.0, 0.0, 1.0, 0.0};
		private float[] Plane = {0,1,0,0};			// the plane is simple here, this is the normal for the plane, 0,1,0
		private float[] LightAmbient = {0.2f, 0.2f, 0.2f, 1.0f};
		private float[] LightDiffuse = {1.0f, 1.0f, 1.0f, 1.0f};
		private float[] LightSpecular = {1.0f, 1.0f, 1.0f, 1.0f};
		private float[] LightPosition = {2.0f, 5.1f, 2.0f, 1.0f};
		private float _shininess = 1.0f * 128;
		private static float viewerPositionY = 3.0f;
		private float rotation = 0.0f;

		// shadow matrix
		private float[] fShadowMatrix = new float[16];
		private int frame = 0;

		public void Run()
		{
			Glut.glutInit();
			Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_RGB | Glut.GLUT_DEPTH);
			Glut.glutInitWindowSize(widthScreen, heightScreen);
			Glut.glutCreateWindow("Moonwalker v0.2");
			Glut.glutFullScreen();

			InitializeObjects();

			Glut.glutDisplayFunc(new Glut.DisplayCallback(OnDisplay));
			Glut.glutIdleFunc(new Glut.IdleCallback(OnIdle));
			Glut.glutReshapeFunc(new Glut.ReshapeCallback(OnReshape));
			Glut.glutKeyboardFunc(new Glut.KeyboardCallback(OnKeyboard));

			Glut.glutMainLoop();
		}

		private void InitializeObjects()
		{
			Gl.glDisable(Gl.GL_DEPTH_TEST); 									 // Выключаем тест глубины
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			Gl.glShadeModel(Gl.GL_FLAT);
			Gl.glShadeModel(Gl.GL_SMOOTH);

			Gl.glEnable(Gl.GL_DEPTH_TEST);                                      // Enables Depth Testing
			Gl.glDepthFunc(Gl.GL_LEQUAL);                                       // The Type Of Depth Testing To Do
			Gl.glClearDepth(1);                                                 // Depth Buffer Setup
			Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);
			Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

			Gl.glEnable(Gl.GL_NORMALIZE);
			Gl.glEnable(Gl.GL_COLOR_MATERIAL);

			SetShadowMatrix(fShadowMatrix, LightPosition, Plane);
		}

		private void InitializeScene()
		{
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			Gl.glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			Gl.glClearDepth(1);                                                 // Depth Buffer Setup
            Gl.glLoadIdentity();
			Glu.gluLookAt(0.0f, 1.0f, 5.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f);

			Gl.glOrtho(0, 5, 0, 5, 0, 5);
            Gl.glTranslated(5, -2, 5);
		}

    	private void OnDisplay()
		{
		    InitializeScene();
			RenderScene();
            rotation += 1.0f;
            frame++;
            Gl.glFlush();
			Glut.glutSwapBuffers();
		}

        private void RenderScene()
		{
			InitializeLights();

            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glColorMask(Gl.GL_FALSE, Gl.GL_FALSE, Gl.GL_FALSE, Gl.GL_FALSE);
		    Gl.glDepthMask(Gl.GL_FALSE);

            Gl.glEnable(Gl.GL_STENCIL_TEST);
            Gl.glStencilFunc(Gl.GL_ALWAYS, 1, 0xFFFFFFFF);
            Gl.glStencilOp(Gl.GL_REPLACE, Gl.GL_REPLACE, Gl.GL_REPLACE);

            RenderFloor();

            Gl.glColorMask(Gl.GL_TRUE, Gl.GL_TRUE, Gl.GL_TRUE, Gl.GL_TRUE);
            Gl.glDepthMask(Gl.GL_TRUE);

            Gl.glStencilFunc(Gl.GL_EQUAL, 1, 0xFFFFFFFF);
            Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_KEEP);

            RenderFloor();

            Gl.glPushMatrix();
                Gl.glColor4f(0.0f, 0.0f, 0.0f, 0.5f);
                Gl.glDisable(Gl.GL_TEXTURE);
                Gl.glDisable(Gl.GL_TEXTURE_2D);
                Gl.glDisable(Gl.GL_LIGHTING);
                Gl.glDisable(Gl.GL_DEPTH_TEST);
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glStencilOp(Gl.GL_KEEP, Gl.GL_KEEP, Gl.GL_INCR);
                Gl.glMultMatrixf(fShadowMatrix);
                Gl.glPushMatrix();
                    RenderFrame(true);
                Gl.glPopMatrix();
                Gl.glEnable(Gl.GL_TEXTURE);
                Gl.glEnable(Gl.GL_DEPTH_TEST);
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glPopMatrix();
            Gl.glDisable(Gl.GL_STENCIL_TEST);

            Gl.glPushMatrix();
                RenderFrame(false);
            Gl.glPopMatrix();

			Gl.glPushMatrix();
                RenderFrame(false);
            Gl.glPopMatrix();
        }

        private void RenderFloor()
        {
            Gl.glColor3f(1.0f, 1.0f, 1.0f);
            DrawFloor(0, 0, 0);
        }

        private void RenderFrame(bool hasShadow)
		{
			if (hasShadow)
			{
				Gl.glColor4f(0.0f, 0.0f, 0.0f, 0.1f);
			}
			Gl.glPushMatrix();
				if (!hasShadow)
				{
					Gl.glColor3f(0.0f, 1.0f, 0.0f);
				}
				Gl.glRotatef(rotation, 0.0f, 1.0f, 0.0f);
				Gl.glTranslatef(0.0f, 2.0f, 0.0f);
				DrawCube(0, 1, 0);
			Gl.glPopMatrix();
        }

		private void InitializeLights()
		{
			Gl.glLightiv(Gl.GL_LIGHT0, Gl.GL_SPOT_EXPONENT, new int[] { 128 });
            Gl.glLightiv(Gl.GL_LIGHT0, Gl.GL_SPOT_CUTOFF, new int[] { 180 });
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_AMBIENT, LightAmbient);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, LightDiffuse);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, LightSpecular);

			Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_LIGHT0);
		}

		private void OnKeyboard(byte key, int x, int y)
		{
			if (key == 27)
			{
				Environment.Exit(0);
			}
		}

	    private void DrawFloor(int fCenterX, int fCenterY, int fCenterZ)
		{
			Gl.glPushMatrix();
				// Gl.glMaterialf(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, _shininess);
				Gl.glBegin(Gl.GL_QUADS);
					Gl.glNormal3f(0.0f, 1.0f, 0.0f);
					float x = fCenterX - 5.0f, z = fCenterZ - 7.0f;

					for (float i = 0.0f; i < 20.0f; i++, x += 1.0f)
					{
						for (float j = 0.0f; j < 24.0f; j += 1.0f, z += 1.0f)
						{
							// draw the plane slightly offset so the shadow shows up
							Gl.glTexCoord2f(0.0f, 0.0f);
							Gl.glVertex3f(x, fCenterY, z);
							Gl.glTexCoord2f(1.0f, 0.0f);
							Gl.glVertex3f(x + 1.0f, fCenterY, z);
							Gl.glTexCoord2f(1.0f, 1.0f);
							Gl.glVertex3f(x + 1.0f, fCenterY, z + 1.0f);
							Gl.glTexCoord2f(0.0f, 1.0f);
							Gl.glVertex3f(x, fCenterY, z + 1.0f);
						}
						z = fCenterZ - 7.0f;
					}
				Gl.glEnd();
			Gl.glPopMatrix();
		}

		private void DrawCube(float x, float y, float z)
		{
			Gl.glPushMatrix();
				Gl.glMaterialf(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, _shininess);
				Gl.glBegin(Gl.GL_QUADS);
					// top
					Gl.glNormal3f(0.0f, 1.0f, 0.0f);
					Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(1.0f+y), (float)(1.0f+z));
					Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3f((float)(1.0f+x), (float)(1.0f+y), (float)(1.0f+z));
					Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3f((float)(1.0f+x), (float)(1.0f+y), (float)(-1.0f+z));
					Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(1.0f+y), (float)(-1.0f+z));

					// bottom
					Gl.glNormal3f(0.0f, -1.0f, 0.0f);
					Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3f((float)(-1.0f+x),(float)( -1.0f+y), (float)(-1.0f+z));
					Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3f((float)(1.0f+x), (float)(-1.0f+y), (float)(-1.0f+z));
					Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3f((float)(1.0f+x), (float)(-1.0f+y), (float)(1.0f+z));
					Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(-1.0f+y), (float)(1.0f+z));

					// left
					Gl.glNormal3f(-1.0f, 0.0f, 0.0f);
					Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(-1.0f+y), (float)(-1.0f+z));
					Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(-1.0f+y), (float)(1.0f+z));
					Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(1.0f+y), (float)(1.0f+z));
					Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3f((float)(-1.0f+x), (float)(1.0f+y), (float)(-1.0f+z));

					// right
					Gl.glNormal3f(1.0f, 0.0f, 0.0f);
					Gl.glTexCoord2f(0.0f, 0.0f); Gl.glVertex3f((float)(1.0f+x), (float)( -1.0f+y),(float)( 1.0f+z));
					Gl.glTexCoord2f(1.0f, 0.0f); Gl.glVertex3f((float)(1.0f+x), (float)(-1.0f+y), (float)(-1.0f+z));
					Gl.glTexCoord2f(1.0f, 1.0f); Gl.glVertex3f((float)(1.0f+x), (float)(1.0f+y), -(float)(1.0f+z));
					Gl.glTexCoord2f(0.0f, 1.0f); Gl.glVertex3f((float)(1.0f+x), (float)(1.0f+y), (float)(1.0f+z));
				Gl.glEnd();
			Gl.glPopMatrix();
		}

		private void SetShadowMatrix(float[] fDestMat,float[] fLightPos,float[] fPlane)
		{
			float dot;

			// dot product of plane and light position
			dot =	fPlane[0] * fLightPos[0] +
					fPlane[1] * fLightPos[1] +
					fPlane[1] * fLightPos[2] +
					fPlane[3] * fLightPos[3];

			// first column
			fDestMat[0] = dot - fLightPos[0] * fPlane[0];
			fDestMat[4] = 0.0f - fLightPos[0] * fPlane[1];
			fDestMat[8] = 0.0f - fLightPos[0] * fPlane[2];
			fDestMat[12] = 0.0f - fLightPos[0] * fPlane[3];

			// second column
			fDestMat[1] = 0.0f - fLightPos[1] * fPlane[0];
			fDestMat[5] = dot - fLightPos[1] * fPlane[1];
			fDestMat[9] = 0.0f - fLightPos[1] * fPlane[2];
			fDestMat[13] = 0.0f - fLightPos[1] * fPlane[3];

			// third column
			fDestMat[2] = 0.0f - fLightPos[2] * fPlane[0];
			fDestMat[6] = 0.0f - fLightPos[2] * fPlane[1];
			fDestMat[10] = dot - fLightPos[2] * fPlane[2];
			fDestMat[14] = 0.0f - fLightPos[2] * fPlane[3];

			// fourth column
			fDestMat[3] = 0.0f - fLightPos[3] * fPlane[0];
			fDestMat[7] = 0.0f - fLightPos[3] * fPlane[1];
			fDestMat[11] = 0.0f - fLightPos[3] * fPlane[2];
			fDestMat[15] = dot - fLightPos[3] * fPlane[3];
		}

		private void OnIdle()
		{
			// render the scene
			Glut.glutPostRedisplay();
			// TODO: Update positions of our models.

		}

		private void OnReshape(int width, int height)
		{
			// save the new window size
            widthScreen = width;
            heightScreen = height;
            // map the view port to the client area
            Gl.glViewport(0, 0, width, height);
            // set the matrix mode to project
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            // load the identity matrix
            Gl.glLoadIdentity();
            // create the viewing frustum
            Glu.gluPerspective(45.0, (float) width / (float) height, 1.0, 300.0);
            // set the matrix mode to modelview
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            // load the identity matrix
            Gl.glLoadIdentity();
            // position the view point
            Glu.gluLookAt(0.0f, viewerPositionY, 5.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f);
		}
	}
}

