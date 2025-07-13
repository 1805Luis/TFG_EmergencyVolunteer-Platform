import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import Login from './routes/Login.jsx'
import Signup from './routes/Signup.jsx'
import Dashboard from './routes/Dashboard.jsx'
import ProtectedRoute from './routes/ProtectedRoute.jsx'
import Perfil from './routes/Profile.jsx'
import Bitacora from './routes/Bitacora.jsx'
import AdministrarUsuarios from './routes/AdministrarUsuarios.jsx'
import GruposRecursos from './routes/GruposRecursos.jsx'
import MisAsignaciones from './routes/MisAsignaciones.jsx'
import { AuthProvider } from './auth/AuthProvider.jsx'
import { ToastContainer } from "react-toastify";
import 'react-toastify/dist/ReactToastify.css'; 
import ProtectedLayout from './pages/ProtectedLayout.jsx'
import FormularioEmergencias from './routes/FormularioEmergencias.jsx'
import GruposTarea from './routes/GruposTarea.jsx'

const router = createBrowserRouter ([
  {path: "/", element: <Login/>, },
  {path: "/signup", element: <Signup/>, },
  {
    path: "/", element: <ProtectedRoute/>,
    children: [
      {
        element: <ProtectedLayout />, 
        children: [ 
          { path: "/dashboard",                  element: <Dashboard/>},
          { path: "/perfil",                     element:<Perfil/>},
          { path: "/seguridad/usuarios",         element: <AdministrarUsuarios/>},
          { path: "/seguridad/registros",        element:<Bitacora/>},
          { path: "/asignaciones",               element:<MisAsignaciones/>},
          { path: "/asignaciones/grupoRecursos", element:<GruposRecursos/>},
          { path: "/formulario/emergencia",      element:<FormularioEmergencias/>},
          { path: "/grupos/tarea",               element:<GruposTarea/> },
         
        ], 
      },
        
    ]
  },
]);

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <AuthProvider>
      <RouterProvider router={router} />
      <ToastContainer
        position="top-right"
        autoClose={3000}
        hideProgressBar={false}
        newestOnTop
        closeOnClick
        pauseOnHover
        draggable
      />
    </AuthProvider>
  </StrictMode>
);
