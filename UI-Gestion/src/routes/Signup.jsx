import { Navigate } from "react-router-dom";
import DefaultLayout from "../pages/DefaultLayout";
import { useAuth } from "../auth/AuthProvider";
import MultiStepForm from "../components/MultiStepForm";


export default function Signup(){
    const auth = useAuth()

    if(auth.isAuthenticated){
        return <Navigate to="/dashboard"/>
    }
    
    return (
        <DefaultLayout>
            <div className="page-wrapper">
                <MultiStepForm/>   
            </div>
        </DefaultLayout>
        
        )
}