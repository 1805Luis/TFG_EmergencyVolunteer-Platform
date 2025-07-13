import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthProvider";
import DefaultLayout from "../pages/DefaultLayout";
import { useState } from "react";
import ApiService from "../services/ApiService";
import ApiError from "../services/ApiError";
import Notificar from "../utils/Notificaciones";
import Spinner from "../components/Spinner";
import Modal from "../components/Modal";


export default function Login(){
    const [formData, setFormData] = useState({
        correo: "",
        clave: "",
    });
    const [isCargando, setIsCargando] = useState(false);

    const location = useLocation();
    const navigate = useNavigate();
    const { saveUser } = useAuth();


    function handleChange(e) {
        const { name, value } = e.target;
        setFormData(prev => ({ ...prev, [name]: value }));
    }

    const from = location.state?.from?.pathname || "/dashboard";
        
    async function handleLogin(e) {
        e.preventDefault(); //Evita que el formulario recargue la página.
        const { correo, clave } = formData;

        if (!correo || !clave) {
            Notificar.Advertencia("Por favor, completa todos los campos.");
            return;
        }

        setIsCargando(true);
    
        try {
            const result= await ApiService.post("/auth/login", { correo, clave });

            const data = result.data;
            
            if(data.token && data.refreshToken){
                Notificar.Exito("Login exitoso");
                saveUser(data)
                navigate(from);
            }
            
        } catch (error) {
            if (error instanceof ApiError) {
                Notificar.Error("Error API: " + error.message);
            } else {
                Notificar.Error("Error API: " + error.message);

            }
        } finally{
            setIsCargando(false);
        }
    }


    return (
        <DefaultLayout>
            <div className="page-wrapper">   

                {isCargando && (
                <Modal isOpen={true} closeModal={() => {}} textoBoton="">
                    <div style={{ textAlign: "center", padding: "20px" }}>
                    <p>🛰 Escaneando tu identidad...</p>
                    <Spinner/>
                    </div>
                </Modal>
                )}


                <form className="form" onSubmit={handleLogin}>
                    <h1>Login</h1>

                    <label>correo</label>
                    <input 
                      type="text" 
                      name="correo"
                      value={formData.correo}
                      onChange={handleChange}
                      autoComplete="username" 
                    />

                    <label>Password</label>
                    <input 
                      type="password" 
                      name="clave"
                      value={formData.clave}
                      onChange={handleChange}
                      autoComplete="current-password"
                    />

                    <button type="submit">Login</button>
                </form>
            </div>
        </DefaultLayout>
    )
}