import { useContext, createContext, useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";
import ApiService from "../services/ApiService";


const AuthContext = createContext({
    isAuthenticated: false,
    loading: true,
    getAccessToken: () =>{},
    saveUser: (userData) => {},
    getRefreshToken: ()=>{},
    getRol: () => {},
    logout: () => {},
    checkAuth : () => {},
    getUsuarioId: () => {},
});

export function AuthProvider({children}){
    const [isAuthenticated, setIsAuthenticated] = useState(false)
    const [accessToken, setAccessToken] = useState("");
    const [rol, setRol] = useState(() => { return localStorage.getItem("rol") || null;});
    const [loading, setLoading] = useState(true);



    useEffect(() => {
        const init = async () => {
        await checkAuth();
        setLoading(false);
        };
        init();
    }, []);

    
    async function checkAuth() {
        const localRefreshToken = getRefreshToken();
        const localAccessToken = accessToken;

        if (localAccessToken && !isTokenExpired(localAccessToken)) {
            setIsAuthenticated(true);
            return;
        }
        if (localRefreshToken) {
            const tokens = await requestNewAccessToken(localRefreshToken);
            if (tokens) {
                saveSessionInfo(tokens.token, localRefreshToken);
            } else {
                logout();
            }
        } else {
            logout();
        }
    }

    function logout() {
        setAccessToken(null);
        localStorage.removeItem("rol");
        localStorage.removeItem("refreshToken");
        setIsAuthenticated(false);
    }

    function isTokenExpired(token) {
        if (!token) return true;
        try {
            const { exp } = jwtDecode(token);
            if (!exp) return true;
            const now = Date.now() / 1000; // en segundos
            return exp < now;
        } catch {
            return true;
        }
    }

    function getUsuarioId(token) {
        try {
            const decoded = jwtDecode(token);
            return decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || null;
        } catch {
            return null;
        }
    }




    async function requestNewAccessToken(refreshToken) {
        try {
            const response = await ApiService.post("/auth/refresh-token", { refreshToken });
            const data = response.data;
            if (data.token) {
            return { token: data.token, refreshToken: data.refreshToken };
            }
            return null;  
        } catch (error) {
            console.error("Error al pedir nuevo token:", error);
            return null;  
        }
    }


    function saveUser(userData){

        saveSessionInfo(userData.token, userData.refreshToken)
    }

    function saveSessionInfo(token, refreshToken) {
        setAccessToken(token);

        const decoded = jwtDecode(token);
        const userRole = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;

        localStorage.setItem("refreshToken", refreshToken);
        localStorage.setItem("rol", userRole);

        setRol(userRole);

        setIsAuthenticated(true);
    }

    function getAccessToken(){
        return accessToken
    }

    function getRefreshToken(){
        return localStorage.getItem("refreshToken");
    }


    function getRol(){
        return localStorage.getItem("rol") || null;
    }


    return <AuthContext.Provider value={{isAuthenticated,loading, getAccessToken, saveUser, getRefreshToken, getRol, logout, checkAuth,getUsuarioId}}>
        {children}
        </AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext);