import { Outlet, Navigate, useLocation } from "react-router-dom";
import { useState } from "react";
import { useAuth } from "../auth/AuthProvider";
import Spinner from "../components/Spinner";

export default function ProtectedRoute(){
    const { isAuthenticated, loading } = useAuth();
    const location = useLocation();

      return loading
    ? <Spinner />
    : isAuthenticated
    ? <Outlet />
    : <Navigate to="/" replace state={{ from: location }} />;
}